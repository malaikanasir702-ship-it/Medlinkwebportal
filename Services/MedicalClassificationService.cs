using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class MedicalClassificationService : IMedicalClassificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiChatService _aiService;
        private readonly ILogger<MedicalClassificationService> _logger;

        public MedicalClassificationService(
            ApplicationDbContext db,
            IAiChatService aiService,
            ILogger<MedicalClassificationService> logger)
        {
            _db = db;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<ClassifiedMedicalRecord?> ClassifyAndPersistAsync(int clinicalDocumentId)
        {
            var doc = await _db.ClinicalDocuments.FindAsync(clinicalDocumentId);
            if (doc == null || string.IsNullOrWhiteSpace(doc.ExtractedJson)) return null;

            string specialty = "General Physician";
            string icdJson = "[]";
            string cptJson = "[]";
            string procJson = "[]";
            string diagJson = "[]";
            double clinicalHours = 1.0;
            double accuracy = 0.95;
            string? notes = null;

            try
            {
                var classifiedJson = await _aiService.ClassifyMedicalDataAsync(doc.ExtractedJson);
                using var jsonDoc = JsonDocument.Parse(classifiedJson);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("specialty", out var specElem))
                    specialty = specElem.GetString() ?? specialty;

                if (root.TryGetProperty("icd10Codes", out var icdElem))
                    icdJson = icdElem.GetRawText();

                if (root.TryGetProperty("cptCodes", out var cptElem))
                    cptJson = cptElem.GetRawText();

                if (root.TryGetProperty("proceduresPerformed", out var procElem))
                    procJson = procElem.GetRawText();

                if (root.TryGetProperty("diagnosisTerms", out var diagElem))
                    diagJson = diagElem.GetRawText();

                if (root.TryGetProperty("clinicalHoursContributed", out var hrsElem))
                    clinicalHours = hrsElem.GetDouble();

                if (root.TryGetProperty("accuracyScore", out var accElem))
                    accuracy = accElem.GetDouble();

                if (root.TryGetProperty("notes", out var notesElem))
                    notes = notesElem.GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse classification JSON. Using heuristics.");
                icdJson = "[\"Z00.00\"]";
                cptJson = "[\"99213\"]";
                procJson = "[\"Clinical Assessment\"]";
                diagJson = "[\"Routine Evaluation\"]";
            }

            // Fallback doctor association if not set on doc
            var doctorUserId = doc.DoctorUserId;
            if (string.IsNullOrEmpty(doctorUserId))
            {
                var fallbackDoctor = await _db.Doctors.FirstOrDefaultAsync();
                doctorUserId = fallbackDoctor?.UserId;
            }

            var record = new ClassifiedMedicalRecord
            {
                ClinicalDocumentId = doc.Id,
                DoctorUserId = doctorUserId,
                PatientUserId = doc.PatientId,
                Specialty = specialty,
                ICD10Codes = icdJson,
                CPTCodes = cptJson,
                ProceduresPerformed = procJson,
                DiagnosisTerms = diagJson,
                ClinicalHoursContributed = clinicalHours > 0 ? clinicalHours : 1.0,
                ProcessedAt = DateTime.UtcNow,
                AccuracyScore = accuracy,
                Notes = notes,
                Status = "Verified"
            };

            _db.ClassifiedRecords.Add(record);
            await _db.SaveChangesAsync();

            // Update Doctor Classification Tags
            if (!string.IsNullOrEmpty(doctorUserId))
            {
                await UpsertTagAsync(doctorUserId, "Specialty", specialty, null);

                try
                {
                    var procedures = JsonSerializer.Deserialize<List<string>>(procJson);
                    if (procedures != null)
                    {
                        foreach (var proc in procedures.Take(5))
                        {
                            await UpsertTagAsync(doctorUserId, "Procedure", proc, null);
                        }
                    }
                }
                catch { }

                try
                {
                    var icdCodes = JsonSerializer.Deserialize<List<string>>(icdJson);
                    if (icdCodes != null)
                    {
                        foreach (var code in icdCodes.Take(5))
                        {
                            await UpsertTagAsync(doctorUserId, "ICD10", code, code);
                        }
                    }
                }
                catch { }

                await _db.SaveChangesAsync();
            }

            return record;
        }

        public async Task<List<ClassifiedMedicalRecord>> GetClassifiedCasesForDoctorAsync(string doctorUserId)
        {
            return await _db.ClassifiedRecords
                .Include(r => r.ClinicalDocument)
                .Include(r => r.Patient)
                .Where(r => r.DoctorUserId == doctorUserId)
                .OrderByDescending(r => r.ProcessedAt)
                .ToListAsync();
        }

        public async Task<List<ClassifiedMedicalRecord>> GetAllClassifiedRecordsAsync(int limit = 100)
        {
            return await _db.ClassifiedRecords
                .Include(r => r.ClinicalDocument)
                .Include(r => r.Doctor)
                .Include(r => r.Patient)
                .OrderByDescending(r => r.ProcessedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<MedicalClassificationTag>> GetTagsForDoctorAsync(string doctorUserId)
        {
            return await _db.MedicalClassificationTags
                .Where(t => t.DoctorUserId == doctorUserId)
                .OrderByDescending(t => t.UsageCount)
                .ToListAsync();
        }

        public async Task<bool> UpdateClassificationAsync(int recordId, string specialty, string[] icdCodes, string[] procedures)
        {
            var record = await _db.ClassifiedRecords.FindAsync(recordId);
            if (record == null) return false;

            record.Specialty = specialty;
            record.ICD10Codes = JsonSerializer.Serialize(icdCodes);
            record.ProceduresPerformed = JsonSerializer.Serialize(procedures);
            record.Status = "ManuallyVerified";

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task UpsertTagAsync(string doctorUserId, string tagType, string tagName, string? tagCode)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;

            var existing = await _db.MedicalClassificationTags
                .FirstOrDefaultAsync(t => t.DoctorUserId == doctorUserId && t.TagType == tagType && t.TagName == tagName);

            if (existing != null)
            {
                existing.UsageCount++;
                existing.LastObservedAt = DateTime.UtcNow;
            }
            else
            {
                _db.MedicalClassificationTags.Add(new MedicalClassificationTag
                {
                    DoctorUserId = doctorUserId,
                    TagType = tagType,
                    TagName = tagName,
                    TagCode = tagCode,
                    UsageCount = 1,
                    LastObservedAt = DateTime.UtcNow
                });
            }
        }
    }
}

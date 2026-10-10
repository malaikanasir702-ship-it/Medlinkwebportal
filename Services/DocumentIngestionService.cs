using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class DocumentIngestionService : IDocumentIngestionService
    {
        private readonly ApplicationDbContext _db;

        public DocumentIngestionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ClinicalDocument> QueueDocumentAsync(string fileUrl, string docType, string? patientId, string? doctorId, string fileName)
        {
            var hash = ComputeSimpleHash(fileUrl + fileName + DateTime.UtcNow.Ticks);

            var document = new ClinicalDocument
            {
                PatientId = patientId,
                DoctorUserId = doctorId,
                DocumentType = string.IsNullOrWhiteSpace(docType) ? "Prescription" : docType,
                FileName = fileName,
                FileUrl = fileUrl,
                OriginalHash = hash,
                UploadDate = DateTime.UtcNow,
                Status = "Pending",
                Confidence = 0.0
            };

            _db.ClinicalDocuments.Add(document);
            await _db.SaveChangesAsync();

            var job = new ExtractionJob
            {
                ClinicalDocumentId = document.Id,
                Status = "Pending",
                RetryCount = 0,
                MaxRetries = 3,
                CreatedAt = DateTime.UtcNow
            };

            _db.ExtractionJobs.Add(job);
            await _db.SaveChangesAsync();

            return document;
        }

        public async Task<ClinicalDocument?> GetDocumentAsync(int documentId)
        {
            return await _db.ClinicalDocuments
                .Include(d => d.Patient)
                .Include(d => d.Doctor)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<List<ClinicalDocument>> GetPatientDocumentsAsync(string patientId)
        {
            return await _db.ClinicalDocuments
                .Where(d => d.PatientId == patientId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<List<ClinicalDocument>> GetDoctorDocumentsAsync(string doctorId)
        {
            return await _db.ClinicalDocuments
                .Where(d => d.DoctorUserId == doctorId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<List<ClinicalDocument>> GetAllDocumentsAsync(int limit = 50)
        {
            return await _db.ClinicalDocuments
                .Include(d => d.Patient)
                .Include(d => d.Doctor)
                .OrderByDescending(d => d.UploadDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> RetryExtractionAsync(int documentId)
        {
            var doc = await _db.ClinicalDocuments.FindAsync(documentId);
            if (doc == null) return false;

            doc.Status = "Pending";
            doc.ErrorMessage = null;

            var existingJob = await _db.ExtractionJobs
                .Where(j => j.ClinicalDocumentId == documentId)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingJob != null)
            {
                existingJob.Status = "Pending";
                existingJob.RetryCount = 0;
                existingJob.ErrorMessage = null;
            }
            else
            {
                _db.ExtractionJobs.Add(new ExtractionJob
                {
                    ClinicalDocumentId = documentId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        private static string ComputeSimpleHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}

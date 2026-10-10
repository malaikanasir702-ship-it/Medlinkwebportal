using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class FhirAdapterService : IFhirAdapterService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDocumentIngestionService _ingestionService;

        public FhirAdapterService(ApplicationDbContext db, IDocumentIngestionService ingestionService)
        {
            _db = db;
            _ingestionService = ingestionService;
        }

        public async Task<object> ExportPatientAsFhirAsync(string patientUserId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == patientUserId);
            if (user == null)
            {
                return new { resourceType = "OperationOutcome", issue = new[] { new { severity = "error", code = "not-found", diagnostics = "Patient not found" } } };
            }

            return new
            {
                resourceType = "Patient",
                id = user.Id,
                identifier = new[]
                {
                    new { system = "https://medlink.health/patient-id", value = user.Id }
                },
                active = true,
                name = new[]
                {
                    new { use = "official", text = $"{user.FirstName} {user.LastName}".Trim() }
                },
                telecom = new[]
                {
                    new { system = "phone", value = user.PhoneNumber },
                    new { system = "email", value = user.Email }
                },
                gender = string.Equals(user.Gender, "female", StringComparison.OrdinalIgnoreCase) ? "female" : "male",
                address = new[]
                {
                    new { city = user.City ?? "Karachi", country = "Pakistan" }
                }
            };
        }

        public async Task<object> ExportDiagnosticReportAsFhirAsync(int reportId)
        {
            var doc = await _db.ClinicalDocuments.Include(d => d.Patient).FirstOrDefaultAsync(d => d.Id == reportId);
            if (doc == null)
            {
                return new { resourceType = "OperationOutcome", issue = new[] { new { severity = "error", code = "not-found", diagnostics = "DiagnosticReport not found" } } };
            }

            return new
            {
                resourceType = "DiagnosticReport",
                id = doc.Id.ToString(),
                status = "final",
                category = new[]
                {
                    new { coding = new[] { new { system = "http://terminology.hl7.org/CodeSystem/v2-0074", code = "LAB", display = "Laboratory" } } }
                },
                code = new
                {
                    coding = new[] { new { system = "http://loinc.org", code = "11502-2", display = doc.DocumentType } },
                    text = doc.FileName
                },
                subject = new { reference = $"Patient/{doc.PatientId}", display = doc.Patient?.UserName },
                effectiveDateTime = doc.UploadDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                conclusion = doc.ExtractedJson
            };
        }

        public async Task<ClinicalDocument> IngestFhirDiagnosticReportAsync(JsonElement fhirJson, string partnerApiKey)
        {
            string fileName = "FHIR_DiagnosticReport_" + DateTime.UtcNow.Ticks;
            string docType = "Diagnostic";
            string fileUrl = "https://medlink.health/fhir/report/" + Guid.NewGuid();
            string? patientId = null;

            if (fhirJson.TryGetProperty("subject", out var subjectElem) && subjectElem.TryGetProperty("reference", out var refElem))
            {
                var refStr = refElem.GetString() ?? "";
                if (refStr.StartsWith("Patient/"))
                {
                    patientId = refStr.Substring("Patient/".Length);
                }
            }

            if (fhirJson.TryGetProperty("code", out var codeElem) && codeElem.TryGetProperty("text", out var textElem))
            {
                fileName = textElem.GetString() ?? fileName;
            }

            var doc = await _ingestionService.QueueDocumentAsync(fileUrl, docType, patientId, null, fileName);
            doc.ExtractedJson = fhirJson.GetRawText();
            await _db.SaveChangesAsync();

            return doc;
        }

        public async Task<ApplicationUser> IngestFhirPatientAsync(JsonElement fhirJson)
        {
            string name = "External Patient";
            string email = $"fhir_{Guid.NewGuid().ToString().Substring(0, 8)}@medlink.health";
            string phone = "+923000000000";

            if (fhirJson.TryGetProperty("name", out var nameElem) && nameElem.GetArrayLength() > 0)
            {
                var first = nameElem[0];
                if (first.TryGetProperty("text", out var tElem))
                    name = tElem.GetString() ?? name;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = name,
                PhoneNumber = phone,
                City = "Karachi",
                EmailConfirmed = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }
    }
}

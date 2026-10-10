using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class DeIdentificationService : IDeIdentificationService
    {
        private readonly IEncryptionService _encryptionService;
        private readonly ApplicationDbContext _db;

        public DeIdentificationService(IEncryptionService encryptionService, ApplicationDbContext db)
        {
            _encryptionService = encryptionService;
            _db = db;
        }

        public DeIdentificationResult ProcessClinicalText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return new DeIdentificationResult();
            }

            var count = 0;
            var text = rawText;

            // Pakistani CNIC
            var cnicMatches = Regex.Matches(text, @"\b\d{5}-?\d{7}-?\d\b");
            count += cnicMatches.Count;
            text = Regex.Replace(text, @"\b\d{5}-?\d{7}-?\d\b", "[CNIC_DEIDENTIFIED]");

            // Phone Numbers
            var phoneMatches = Regex.Matches(text, @"(?:\+92|0092|0)(?:3\d{2}|[2-9]\d{1})\d{7}\b");
            count += phoneMatches.Count;
            text = Regex.Replace(text, @"(?:\+92|0092|0)(?:3\d{2}|[2-9]\d{1})\d{7}\b", "[PHONE_DEIDENTIFIED]");

            // Emails
            var emailMatches = Regex.Matches(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            count += emailMatches.Count;
            text = Regex.Replace(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "[EMAIL_DEIDENTIFIED]");

            var encryptedOriginal = _encryptionService.Encrypt(rawText);

            return new DeIdentificationResult
            {
                DeIdentifiedContent = text,
                EncryptedOriginal = encryptedOriginal,
                PiiItemsRemoved = count,
                ContainsSensitiveData = count > 0
            };
        }

        public async Task<DeIdentificationResult> ProcessDocumentAsync(int documentId)
        {
            var doc = await _db.ClinicalDocuments.FindAsync(documentId);
            if (doc == null || string.IsNullOrEmpty(doc.ExtractedJson))
            {
                return new DeIdentificationResult();
            }

            var result = ProcessClinicalText(doc.ExtractedJson);

            // Log access / governance audit
            _db.DataAccessLogs.Add(new DataAccessLog
            {
                AccessedByRole = "SystemAI",
                PatientUserId = doc.PatientId,
                ResourceName = "ClinicalDocument",
                RecordId = doc.Id.ToString(),
                AccessType = "Deidentify",
                Purpose = "Anonymization for clinical intelligence and knowledge graph",
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return result;
        }
    }
}

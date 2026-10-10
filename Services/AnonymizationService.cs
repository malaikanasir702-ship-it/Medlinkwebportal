using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MedLinkPortal.Models;
using MedLinkPortal.Models.KnowledgeGraph;

namespace MedLinkPortal.Services
{
    public class AnonymizationService : IAnonymizationService
    {
        // Pakistani CNIC Regex: 42101-1234567-1 or 4210112345671
        private static readonly Regex CnicRegex = new Regex(@"\b\d{5}-?\d{7}-?\d\b", RegexOptions.Compiled);

        // Pakistani Phone Regex: 03001234567 or +923001234567
        private static readonly Regex PhoneRegex = new Regex(@"(?:\+92|0092|0)(?:3\d{2}|[2-9]\d{1})\d{7}\b", RegexOptions.Compiled);

        // Email Regex
        private static readonly Regex EmailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);

        public AnonymizedCaseData AnonymizeClassifiedRecord(ClassifiedMedicalRecord record, ApplicationUser? patient)
        {
            var patientId = record.PatientUserId ?? "anonymous";
            var caseHash = HashIdentifier($"{patientId}_{record.Id}_{record.ProcessedAt.Ticks}");

            string ageGroup = "30-39";
            string gender = patient?.Gender ?? "Unknown";
            string city = patient?.City ?? "Karachi";

            string primaryDiag = "General Evaluation";
            try
            {
                var diags = System.Text.Json.JsonSerializer.Deserialize<string[]>(record.DiagnosisTerms);
                if (diags != null && diags.Length > 0)
                {
                    primaryDiag = diags[0];
                }
            }
            catch { }

            return new AnonymizedCaseData
            {
                CaseHash = caseHash,
                AgeGroup = ageGroup,
                Gender = gender,
                City = city,
                PrimaryDiagnosis = primaryDiag,
                DiagnosesJson = record.DiagnosisTerms,
                ProceduresJson = record.ProceduresPerformed,
                MedicationsJson = "[\"Standard Clinical Regimen\"]",
                Outcome = "Managed",
                ClinicalHours = record.ClinicalHoursContributed,
                IngestedAt = DateTime.UtcNow
            };
        }

        public string StripPii(string rawClinicalText)
        {
            if (string.IsNullOrEmpty(rawClinicalText)) return rawClinicalText;

            var cleaned = CnicRegex.Replace(rawClinicalText, "[CNIC_REMOVED]");
            cleaned = PhoneRegex.Replace(cleaned, "[PHONE_REMOVED]");
            cleaned = EmailRegex.Replace(cleaned, "[EMAIL_REMOVED]");

            return cleaned;
        }

        public string HashIdentifier(string identifier)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(identifier));
            var sb = new StringBuilder("P_");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}

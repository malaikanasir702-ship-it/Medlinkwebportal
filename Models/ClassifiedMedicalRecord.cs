using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class ClassifiedMedicalRecord
    {
        [Key]
        public int Id { get; set; }

        public int? ClinicalDocumentId { get; set; }

        [ForeignKey("ClinicalDocumentId")]
        public virtual ClinicalDocument? ClinicalDocument { get; set; }

        public string? DoctorUserId { get; set; }

        [ForeignKey("DoctorUserId")]
        public virtual ApplicationUser? Doctor { get; set; }

        public string? PatientUserId { get; set; }

        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser? Patient { get; set; }

        [MaxLength(100)]
        public string Specialty { get; set; } = "General Practice";

        // JSON array of ICD-10 codes, e.g. ["I21.0", "I25.1"]
        public string ICD10Codes { get; set; } = "[]";

        // JSON array of CPT codes, e.g. ["92928", "93000"]
        public string CPTCodes { get; set; } = "[]";

        // JSON array of procedures performed
        public string ProceduresPerformed { get; set; } = "[]";

        // JSON array of diagnosis terms
        public string DiagnosisTerms { get; set; } = "[]";

        public double ClinicalHoursContributed { get; set; } = 1.0;

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public double AccuracyScore { get; set; } = 0.95;

        public string? Notes { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Verified";
    }
}

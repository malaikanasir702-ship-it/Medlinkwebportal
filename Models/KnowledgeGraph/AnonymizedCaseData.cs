using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models.KnowledgeGraph
{
    public class AnonymizedCaseData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string CaseHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string AgeGroup { get; set; } = "30-39";

        [MaxLength(20)]
        public string Gender { get; set; } = "Unknown";

        [MaxLength(100)]
        public string City { get; set; } = "Karachi";

        [MaxLength(200)]
        public string PrimaryDiagnosis { get; set; } = string.Empty;

        // JSON array of diagnoses
        public string DiagnosesJson { get; set; } = "[]";

        // JSON array of procedures
        public string ProceduresJson { get; set; } = "[]";

        // JSON array of medications
        public string MedicationsJson { get; set; } = "[]";

        [MaxLength(100)]
        public string Outcome { get; set; } = "Recovered";

        public double ClinicalHours { get; set; } = 1.0;

        public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
    }
}

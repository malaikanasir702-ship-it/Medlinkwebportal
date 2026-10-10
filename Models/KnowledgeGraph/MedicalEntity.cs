using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models.KnowledgeGraph
{
    public class MedicalEntity
    {
        [Key]
        public int Id { get; set; }

        // Disease, Procedure, Drug, Symptom, DoctorType, TreatmentOutcome
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = "Disease";

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // ICD-10 or CPT code if applicable
        [MaxLength(50)]
        public string? Code { get; set; }

        // JSON metadata
        public string PropertiesJson { get; set; } = "{}";

        public double Confidence { get; set; } = 1.0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class MedicalClassificationTag
    {
        [Key]
        public int Id { get; set; }

        public string? DoctorUserId { get; set; }

        [ForeignKey("DoctorUserId")]
        public virtual ApplicationUser? Doctor { get; set; }

        [Required]
        [MaxLength(50)]
        public string TagType { get; set; } = "Specialty"; // Specialty, ICD10, CPT, Procedure, Diagnosis

        [Required]
        [MaxLength(150)]
        public string TagName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TagCode { get; set; } // e.g. I21.0 or 92928

        public int UsageCount { get; set; } = 1;

        public DateTime LastObservedAt { get; set; } = DateTime.UtcNow;
    }
}

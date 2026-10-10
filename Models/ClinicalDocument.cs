using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class ClinicalDocument
    {
        [Key]
        public int Id { get; set; }

        public string? PatientId { get; set; }

        public string? DoctorUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = "Prescription"; // Prescription, LabReport, DischargeSummary, CaseHistory, Diagnostic

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FileUrl { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? OriginalHash { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // Structured JSON payload extracted by AI
        public string? ExtractedJson { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

        public string? ErrorMessage { get; set; }

        public double Confidence { get; set; } = 0.0;

        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        [ForeignKey("DoctorUserId")]
        public virtual ApplicationUser? Doctor { get; set; }
    }
}

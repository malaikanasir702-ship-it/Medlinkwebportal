using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class MatchingRequest
    {
        [Key]
        public int Id { get; set; }

        public string? PatientUserId { get; set; }

        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser? PatientUser { get; set; }

        [Required]
        public string Symptoms { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Urgency { get; set; } = "Normal"; // Routine, Urgent, Emergency

        public int? PatientAge { get; set; }

        [MaxLength(20)]
        public string? PatientGender { get; set; }

        [MaxLength(100)]
        public string? PatientLocation { get; set; }

        [MaxLength(100)]
        public string? TargetSpecialty { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Status { get; set; } = "Completed";

        // JSON array of matched doctors with scores and reasons
        public string? ResultsJson { get; set; }
    }
}

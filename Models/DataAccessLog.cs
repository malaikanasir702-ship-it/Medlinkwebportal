using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class DataAccessLog
    {
        [Key]
        public int Id { get; set; }

        public string? AccessedByUserId { get; set; }

        [ForeignKey("AccessedByUserId")]
        public virtual ApplicationUser? AccessedByUser { get; set; }

        [MaxLength(50)]
        public string AccessedByRole { get; set; } = "Doctor";

        public string? PatientUserId { get; set; }

        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser? PatientUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string ResourceName { get; set; } = "MedicalRecord";

        [MaxLength(100)]
        public string? RecordId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccessType { get; set; } = "Read"; // Read, Write, Export, Deidentify

        [MaxLength(60)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(250)]
        public string? Purpose { get; set; } = "Clinical Consultation";
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Areas.Doctor.Models
{
    public class WalkInPatient
    {
        [Key]
        public int Id { get; set; }

        // Which doctor registered this patient
        [Required]
        public string DoctorUserId { get; set; } = string.Empty;

        // Patient basic info (offline — no portal account)
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public int? AgeYears { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; } // Male / Female / Other

        [MaxLength(300)]
        public string? VisitReason { get; set; }

        // Medical record fields (filled when doctor adds record)
        [MaxLength(1000)]
        public string? Diagnosis { get; set; }

        [MaxLength(2000)]
        public string? Prescription { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(200)]
        public string? Vitals { get; set; } // e.g. "BP: 120/80, HR: 72"

        [MaxLength(300)]
        public string? Allergies { get; set; }

        public bool HasRecord { get; set; } = false;

        public DateTime VisitDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

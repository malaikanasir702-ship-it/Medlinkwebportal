using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Areas.Doctor.Models
{
    public class PatientRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [ForeignKey("PatientId")]
        public ApplicationUser? Patient { get; set; }

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [ForeignKey("DoctorId")]
        public ApplicationUser? Doctor { get; set; }

        [Required]
        [Encrypted]
        public string Diagnosis { get; set; } = string.Empty;

        [Encrypted]
        public string? Prescription { get; set; }

        [Encrypted]
        public string? Notes { get; set; }
        
        // Expanded EHR Fields
        [Encrypted]
        public string? Vitals { get; set; } // e.g. "BP: 120/80, HR: 72, Temp: 98.6"
        [Encrypted]
        public string? Allergies { get; set; }
        public string? LabReportPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Areas.Admin.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        [Required]
        public string PatientId { get; set; }
        [Required]
        public string PhysicianId { get; set; }
        [Required]
        public string RecordType { get; set; } // Prescription, LabResult, Imaging
        [Required]
        public string Title { get; set; }
        [Encrypted]
        public string? Content { get; set; } // Text content for prescriptions or notes
        [Encrypted]
        public string? Attachment { get; set; } // Base64 for images/PDFs
        [Required]
        public DateTime DateCreated { get; set; }
        public bool IsApproved { get; set; } // Admin can review and approve
        public Patient? Patient { get; set; }
        public Physician? Physician { get; set; }
    }
}

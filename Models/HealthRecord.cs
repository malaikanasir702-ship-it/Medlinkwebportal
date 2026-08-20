using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class HealthRecord
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } // Laboratory, Radiology, Prescription, Certification

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string Provider { get; set; }

        [StringLength(20)]
        public string FileSize { get; set; }

        [StringLength(20)]
        public string FileType { get; set; }

        [StringLength(500)]
        public string FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

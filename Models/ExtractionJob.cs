using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class ExtractionJob
    {
        [Key]
        public int Id { get; set; }

        public int ClinicalDocumentId { get; set; }

        [ForeignKey("ClinicalDocumentId")]
        public virtual ClinicalDocument? ClinicalDocument { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 3;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public string? ErrorMessage { get; set; }
    }
}

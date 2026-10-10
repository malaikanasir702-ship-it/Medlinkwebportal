using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class IntegrationPartner
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string PartnerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ApiKey { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? WebhookUrl { get; set; }

        // Hospital, Laboratory, Insurance, HealthAgency
        [Required]
        [MaxLength(50)]
        public string PartnerType { get; set; } = "Hospital";

        public bool IsActive { get; set; } = true;

        // JSON array of permitted operations e.g. ["lab-result", "discharge-summary", "fhir"]
        public string PermissionsJson { get; set; } = "[\"lab-result\", \"discharge-summary\"]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastActiveAt { get; set; }
    }
}

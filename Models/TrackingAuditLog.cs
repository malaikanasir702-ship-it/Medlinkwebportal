using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("TrackingAuditLogs")]
    public class TrackingAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;    // 'RiderAssigned', 'StatusChanged', etc.

        [Required]
        [MaxLength(450)]
        public string ActorId { get; set; } = string.Empty;      // userId who performed the action

        [Required]
        [MaxLength(100)]
        public string TargetId { get; set; } = string.Empty;     // orderId or riderId

        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = string.Empty;   // 'PharmacyOrder', 'LabBooking', 'Rider'

        [MaxLength(200)]
        public string? OldValue { get; set; }    // previous status/value

        [MaxLength(200)]
        public string? NewValue { get; set; }    // new status/value

        public string? Metadata { get; set; }    // JSON: extra context

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

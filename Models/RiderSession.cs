using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("RiderSessions")]
    public class RiderSession
    {
        [Key]
        public int Id { get; set; }

        public int RiderId { get; set; }
        [ForeignKey("RiderId")]
        public virtual Rider? RiderProfile { get; set; }

        public int OrderId { get; set; }

        [Required]
        [MaxLength(20)]
        public string OrderType { get; set; } = string.Empty;  // "PharmacyOrder" | "LabBooking"

        public double LastLatitude { get; set; }
        public double LastLongitude { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Telemetry columns (Enhancement 12)
        public double? Heading { get; set; }          // degrees 0–360
        public double? SpeedKmh { get; set; }         // km/h
        public double? AccuracyMeters { get; set; }   // GPS accuracy
        public int? BatteryLevel { get; set; }        // 0–100 %
        public string? ConnectionId { get; set; }     // SignalR connectionId
        public string? DeviceId { get; set; }         // device identifier
        public DateTime? LastHeartbeatAt { get; set; }  // Enhancement 7

        // Enhancement 5: geofence state
        public bool GeofenceTriggered { get; set; } = false;
    }
}

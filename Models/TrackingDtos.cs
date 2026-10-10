using System;

namespace MedLinkPortal.Models
{
    public class LocationUpdateDto
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AccuracyMeters { get; set; }
        public DateTime Timestamp { get; set; }
        public double SpeedKmh { get; set; }
        public double Heading { get; set; }
        public int BatteryLevel { get; set; }
        public string DeviceId { get; set; } = string.Empty;
    }

    public class LocationBroadcastDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public double? Heading { get; set; }
        public int? EstimatedMinutes { get; set; }
        public double? DistanceKm { get; set; }
        public int? RiderId { get; set; }
    }

    public class OrderStatusUpdateDto
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public int NewStatus { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
    }

    public class RiderStatusUpdateRequest
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public int NewStatus { get; set; }
    }

    public class AssignRiderRequest
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public int RiderId { get; set; }
    }

    public class EtaResponseDto
    {
        public int EstimatedMinutes { get; set; }
        public double DistanceKm { get; set; }
        public bool IsFromFallback { get; set; }
    }

    public class RiderInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string? VehicleNumber { get; set; }
        public double AverageRating { get; set; }
    }

    public class RiderOrderDto
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public string PatientAddress { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }
    }

    public class ActiveSessionDto
    {
        public int SessionId { get; set; }
        public int RiderId { get; set; }
        public string RiderName { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public int ElapsedSecondsSinceUpdate { get; set; }
        public bool IsHeartbeatStale { get; set; }
    }

    public class CreateRiderViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Motorcycle";
        public string? VehicleNumber { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class DirectionsResult
    {
        public string EncodedPolyline { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public double DistanceMeters { get; set; }
        public bool IsFromFallback { get; set; }
    }
}

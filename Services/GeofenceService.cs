using System;

namespace MedLinkPortal.Services
{
    /// <summary>
    /// Geofence detection using Haversine formula.
    /// Default trigger radius: 500 meters (Enhancement 5).
    /// No external API required — pure math.
    /// </summary>
    public class GeofenceService
    {
        private const double EarthRadiusMeters = 6_371_000.0;
        public const double DefaultTriggerRadiusMeters = 500.0;

        /// <summary>
        /// Returns true when the rider is within <paramref name="radiusMeters"/> of the destination.
        /// Uses the Haversine formula for great-circle distance.
        /// </summary>
        public bool IsWithinRadius(
            double riderLat, double riderLng,
            double destLat, double destLng,
            double radiusMeters = DefaultTriggerRadiusMeters)
        {
            double distance = HaversineDistance(riderLat, riderLng, destLat, destLng);
            return distance <= radiusMeters;
        }

        /// <summary>
        /// Computes straight-line distance in meters between two GPS coordinates
        /// using the Haversine formula.
        /// </summary>
        public double HaversineDistance(
            double lat1, double lng1,
            double lat2, double lng2)
        {
            double dLat = ToRadians(lat2 - lat1);
            double dLng = ToRadians(lng2 - lng1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMeters * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}

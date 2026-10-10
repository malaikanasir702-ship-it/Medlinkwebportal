using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedLinkPortal.Services
{
    /// <summary>
    /// Provides real route + ETA using Google Directions API (Bonus Feature 1).
    /// Falls back to Haversine formula + assumed 30 km/h speed when API key is
    /// not configured or the request fails.
    /// </summary>
    public class GoogleDirectionsService
    {
        private const double AssumedSpeedKmh = 30.0;
        private const double EarthRadiusMeters = 6_371_000.0;
        private const string DirectionsApiUrl = "https://maps.googleapis.com/maps/api/directions/json";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDirectionsService> _logger;

        public GoogleDirectionsService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GoogleDirectionsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Returns route directions (encoded polyline, ETA, distance).
        /// Primary: Google Directions API with departure_time=now for traffic-aware ETA.
        /// Fallback: Haversine distance / 30 km/h.
        /// </summary>
        public async Task<DirectionsResult> GetDirectionsAsync(
            double fromLat, double fromLng,
            double toLat, double toLng)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    return await CallGoogleApiAsync(fromLat, fromLng, toLat, toLng, apiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Google Directions API failed — falling back to Haversine");
                }
            }

            return BuildHaversineFallback(fromLat, fromLng, toLat, toLng);
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private async Task<DirectionsResult> CallGoogleApiAsync(
            double fromLat, double fromLng,
            double toLat, double toLng,
            string apiKey)
        {
            var url = $"{DirectionsApiUrl}"
                    + $"?origin={fromLat},{fromLng}"
                    + $"&destination={toLat},{toLng}"
                    + $"&departure_time=now"
                    + $"&key={apiKey}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var document = JsonNode.Parse(json);

            var status = document?["status"]?.GetValue<string>();
            if (status != "OK")
                throw new Exception($"Google Directions API returned status: {status}");

            var route = document!["routes"]![0]!;
            var leg = route["legs"]![0]!;

            // Prefer duration_in_traffic if available, otherwise use duration
            var durationNode = leg["duration_in_traffic"] ?? leg["duration"];
            int durationSecs = durationNode?["value"]?.GetValue<int>() ?? 0;
            int distanceMeters = leg["distance"]?["value"]?.GetValue<int>() ?? 0;

            // Encoded polyline (overview_polyline)
            var polyline = route["overview_polyline"]?["points"]?.GetValue<string>() ?? string.Empty;

            return new DirectionsResult
            {
                EncodedPolyline = polyline,
                DurationSeconds = durationSecs,
                DistanceMeters = distanceMeters,
                IsFromFallback = false
            };
        }

        private DirectionsResult BuildHaversineFallback(
            double fromLat, double fromLng,
            double toLat, double toLng)
        {
            double distanceMeters = HaversineDistance(fromLat, fromLng, toLat, toLng);
            double durationSecs = (distanceMeters / 1000.0) / AssumedSpeedKmh * 3600.0;

            // Straight-line 2-point encoded polyline (start → end)
            // Using a simple encoding for two points
            string fallbackPolyline = EncodeTwoPointPolyline(fromLat, fromLng, toLat, toLng);

            return new DirectionsResult
            {
                EncodedPolyline = fallbackPolyline,
                DurationSeconds = (int)Math.Ceiling(durationSecs),
                DistanceMeters = distanceMeters,
                IsFromFallback = true
            };
        }

        private static double HaversineDistance(
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

        /// <summary>
        /// Encodes two GPS points using the Google Polyline Encoding algorithm.
        /// Used as a fallback when the Directions API is unavailable.
        /// </summary>
        private static string EncodeTwoPointPolyline(double lat1, double lng1, double lat2, double lng2)
        {
            return EncodePoint(lat1, lng1, 0, 0) + EncodePoint(lat2, lng2, lat1, lng1);
        }

        private static string EncodePoint(double lat, double lng, double prevLat, double prevLng)
        {
            int eLat = EncodeValue((int)Math.Round((lat - prevLat) * 1e5));
            int eLng = EncodeValue((int)Math.Round((lng - prevLng) * 1e5));
            return EncodeChunks(eLat) + EncodeChunks(eLng);
        }

        private static int EncodeValue(int value)
        {
            int shifted = value << 1;
            return value < 0 ? ~shifted : shifted;
        }

        private static string EncodeChunks(int value)
        {
            var result = string.Empty;
            while (value >= 0x20)
            {
                result += (char)((0x20 | (value & 0x1F)) + 63);
                value >>= 5;
            }
            result += (char)(value + 63);
            return result;
        }
    }
}

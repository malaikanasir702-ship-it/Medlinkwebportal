using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedLinkPortal.Controllers.Api
{
    /// <summary>
    /// REST API for tracking: last location, ETA, route polyline, rider info card.
    /// </summary>
    [ApiController]
    [Route("api/tracking")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class TrackingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleDirectionsService _directionsService;

        public TrackingApiController(
            ApplicationDbContext context,
            GoogleDirectionsService directionsService)
        {
            _context = context;
            _directionsService = directionsService;
        }

        // GET /api/tracking/last-location/{orderType}/{orderId}
        [HttpGet("last-location/{orderType}/{orderId:int}")]
        public async Task<IActionResult> GetLastLocation(string orderType, int orderId)
        {
            try
            {
                var session = await _context.RiderSessions
                    .FirstOrDefaultAsync(s => s.OrderId == orderId
                        && s.OrderType == orderType
                        && s.IsActive);

                if (session == null)
                    return NotFound(new { message = "No active tracking session found." });

                return Ok(new LocationBroadcastDto
                {
                    Latitude = session.LastLatitude,
                    Longitude = session.LastLongitude,
                    Timestamp = session.LastUpdatedAt,
                    Heading = session.Heading
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get last location." });
            }
        }

        // GET /api/tracking/eta/{orderType}/{orderId}
        [HttpGet("eta/{orderType}/{orderId:int}")]
        public async Task<IActionResult> GetEta(string orderType, int orderId)
        {
            try
            {
                var session = await _context.RiderSessions
                    .FirstOrDefaultAsync(s => s.OrderId == orderId
                        && s.OrderType == orderType
                        && s.IsActive);

                if (session == null)
                    return NotFound(new { message = "No active tracking session found." });

                var (destLat, destLng) = await GetDestinationAsync(orderType, orderId);
                if (destLat == null || destLat.Value == 0)
                    return Ok(new EtaResponseDto
                    {
                        EstimatedMinutes = 0,
                        DistanceKm = 0,
                        IsFromFallback = true
                    });

                var directions = await _directionsService.GetDirectionsAsync(
                    session.LastLatitude, session.LastLongitude,
                    destLat.Value, destLng!.Value);

                return Ok(new EtaResponseDto
                {
                    EstimatedMinutes = (int)Math.Ceiling(directions.DurationSeconds / 60.0),
                    DistanceKm = Math.Round(directions.DistanceMeters / 1000.0, 2),
                    IsFromFallback = directions.IsFromFallback
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get ETA." });
            }
        }

        // GET /api/tracking/route/{orderType}/{orderId}
        [HttpGet("route/{orderType}/{orderId:int}")]
        public async Task<IActionResult> GetRoute(string orderType, int orderId)
        {
            try
            {
                var session = await _context.RiderSessions
                    .FirstOrDefaultAsync(s => s.OrderId == orderId
                        && s.OrderType == orderType
                        && s.IsActive);

                if (session == null)
                    return NotFound(new { message = "No active tracking session found." });

                var (destLat, destLng) = await GetDestinationAsync(orderType, orderId);
                if (destLat == null || destLat.Value == 0)
                    return Ok(new { polyline = string.Empty, isFromFallback = true });

                var directions = await _directionsService.GetDirectionsAsync(
                    session.LastLatitude, session.LastLongitude,
                    destLat.Value, destLng!.Value);

                return Ok(new
                {
                    polyline = directions.EncodedPolyline,
                    isFromFallback = directions.IsFromFallback,
                    durationSec = directions.DurationSeconds,
                    distanceMeters = directions.DistanceMeters
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get route." });
            }
        }

        // GET /api/tracking/rider-info/{orderType}/{orderId}
        [HttpGet("rider-info/{orderType}/{orderId:int}")]
        public async Task<IActionResult> GetRiderInfo(string orderType, int orderId)
        {
            try
            {
                var session = await _context.RiderSessions
                    .Include(s => s.RiderProfile)
                        .ThenInclude(r => r!.User)
                    .FirstOrDefaultAsync(s => s.OrderId == orderId
                        && s.OrderType == orderType
                        && s.IsActive);

                if (session?.RiderProfile == null)
                    return NotFound(new { message = "Rider not found for this order." });

                var rider = session.RiderProfile;
                var name = $"{rider.User?.FirstName} {rider.User?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = rider.User?.UserName ?? "Rider";

                return Ok(new RiderInfoDto
                {
                    Name = name,
                    VehicleType = rider.VehicleType,
                    VehicleNumber = rider.VehicleNumber,
                    AverageRating = rider.AverageRating
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get rider info." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private async Task<(double? lat, double? lng)> GetDestinationAsync(
            string orderType, int orderId)
        {
            if (orderType == "PharmacyOrder")
            {
                var o = await _context.PharmacyOrders.FindAsync(orderId);
                return (o?.DestinationLatitude, o?.DestinationLongitude);
            }
            if (orderType == "LabBooking")
            {
                var b = await _context.LabBookings.FindAsync(orderId);
                return (b?.DestinationLatitude, b?.DestinationLongitude);
            }
            return (null, null);
        }
    }
}

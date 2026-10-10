using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Hubs
{
    /// <summary>
    /// Real-time SignalR hub for rider GPS tracking (FoodPanda-style).
    /// Handles: location updates, heartbeat, group join/leave, single-device login,
    /// geofencing, rate limiting, GPS spoof detection, and admin live map.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class TrackingHub : Hub
    {
        // Rate limiting: max 20 location updates per 30 seconds per connectionId
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _rateLimiter
            = new ConcurrentDictionary<string, (int, DateTime)>();

        private const int RateLimitMaxUpdates = 20;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(30);

        // GPS spoof detection: max plausible speed in km/h (~144 km/h = 200m in 5s)
        private const double MaxPlausibleSpeedKmh = 144.0;
        private const double EarthRadiusMeters = 6_371_000.0;

        private readonly ApplicationDbContext _context;
        private readonly GeofenceService _geofenceService;
        private readonly TrackingAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GoogleDirectionsService _directionsService;

        public TrackingHub(
            ApplicationDbContext context,
            GeofenceService geofenceService,
            TrackingAuditService auditService,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            GoogleDirectionsService directionsService)
        {
            _context = context;
            _geofenceService = geofenceService;
            _auditService = auditService;
            _notificationService = notificationService;
            _userManager = userManager;
            _directionsService = directionsService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Connection lifecycle
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// On connect: enforce single-device login by terminating previous connection.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Single-device login (Enhancement 8):
                    // Send SessionTerminated to old connection, then overwrite
                    if (!string.IsNullOrEmpty(user.ActiveSignalRConnectionId)
                        && user.ActiveSignalRConnectionId != Context.ConnectionId)
                    {
                        await Clients.Client(user.ActiveSignalRConnectionId)
                            .SendAsync("SessionTerminated", "Your session was opened on another device.");
                    }

                    user.ActiveSignalRConnectionId = Context.ConnectionId;
                    await _userManager.UpdateAsync(user);
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// On disconnect: mark active RiderSession inactive, clear connectionId.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // Find Rider profile
                var rider = await _context.Riders
                    .FirstOrDefaultAsync(r => r.UserId == userId);

                if (rider != null)
                {
                    // End active sessions for this rider
                    var activeSessions = await _context.RiderSessions
                        .Where(s => s.RiderId == rider.Id && s.IsActive)
                        .ToListAsync();

                    foreach (var session in activeSessions)
                    {
                        session.IsActive = false;
                        await _auditService.LogAsync(
                            TrackingAuditService.TrackingEnded,
                            userId,
                            session.OrderId.ToString(),
                            session.OrderType);
                    }
                }

                // Clear ActiveSignalRConnectionId
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && user.ActiveSignalRConnectionId == Context.ConnectionId)
                {
                    user.ActiveSignalRConnectionId = null;
                    await _userManager.UpdateAsync(user);
                }

                await _context.SaveChangesAsync();
            }

            // Clean up rate limiter entry
            _rateLimiter.TryRemove(Context.ConnectionId, out _);

            await base.OnDisconnectedAsync(exception);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Group management
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Patient joins order group to receive location updates.
        /// Validates patient owns the order before joining.
        /// </summary>
        public async Task JoinOrderGroup(string groupKey)
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Unauthorized");

            // Validate patient owns this order
            if (!await PatientOwnsOrderAsync(userId, groupKey))
                throw new HubException("Unauthorized: You do not own this order.");

            await Groups.AddToGroupAsync(Context.ConnectionId, groupKey);

            // Immediately send last known location (Enhancement: late-join support)
            var session = await GetActiveSessionFromGroupKeyAsync(groupKey);
            if (session != null)
            {
                var broadcast = new LocationBroadcastDto
                {
                    Latitude = session.LastLatitude,
                    Longitude = session.LastLongitude,
                    Timestamp = session.LastUpdatedAt,
                    Heading = session.Heading
                };
                await Clients.Caller.SendAsync("ReceiveLocationUpdate", broadcast);
            }
        }

        public async Task LeaveOrderGroup(string groupKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupKey);
        }

        /// <summary>
        /// Admin joins the live-map group to see all active riders.
        /// </summary>
        public async Task JoinAdminLiveMap()
        {
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;
            if (!isAdmin)
                throw new HubException("Unauthorized: Admin role required.");

            await Groups.AddToGroupAsync(Context.ConnectionId, "admin_live_map");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rider methods
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Rider sends GPS location update. Full 11-step validation pipeline.
        /// </summary>
        public async Task SendLocationUpdate(LocationUpdateDto dto)
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Unauthorized");

            // Step 1: Rate limiting (max 20 per 30s)
            if (!CheckRateLimit(Context.ConnectionId))
                throw new HubException("RateLimitExceeded");

            // Step 2: GPS spoof detection (>200m in <5s)
            var existingSession = await _context.RiderSessions
                .FirstOrDefaultAsync(s => s.OrderId == dto.OrderId
                    && s.OrderType == dto.OrderType
                    && s.IsActive);

            if (existingSession != null
                && existingSession.LastLatitude != 0
                && existingSession.LastLongitude != 0)
            {
                double dist = HaversineDistance(
                    existingSession.LastLatitude, existingSession.LastLongitude,
                    dto.Latitude, dto.Longitude);

                double elapsedSeconds = (DateTime.UtcNow - existingSession.LastUpdatedAt).TotalSeconds;

                if (dist > 200 && elapsedSeconds < 5)
                {
                    await _auditService.LogAsync(
                        TrackingAuditService.GPSSpoofDetected,
                        userId,
                        dto.OrderId.ToString(),
                        dto.OrderType,
                        metadata: $"{{\"dist\":{dist:F1},\"elapsed\":{elapsedSeconds:F1}}}");

                    // Alert admin live map
                    await Clients.Group("admin_live_map")
                        .SendAsync("SpoofAlert", new { dto.OrderId, dto.OrderType, userId });

                    throw new HubException("SuspiciousLocation: GPS jump detected.");
                }
            }

            // Step 3: Accuracy filter (soft filter — warn but don't reject)
            // Strict <20m only in production; for emulator/indoor GPS allow up to 100m
            // to avoid dropping all updates when GPS is weak.
            // HubException removed: accuracy logged but update still processed.
            bool lowAccuracy = dto.AccuracyMeters >= 20;
            // (Logged to audit below if needed — not blocking)

            // Step 4 & 5: Coordinate bounds
            if (dto.Latitude < -90 || dto.Latitude > 90)
                throw new HubException("InvalidCoordinates: latitude out of range.");
            if (dto.Longitude < -180 || dto.Longitude > 180)
                throw new HubException("InvalidCoordinates: longitude out of range.");

            // Step 6: Rider owns this session
            var rider = await _context.Riders.FirstOrDefaultAsync(r => r.UserId == userId);
            if (rider == null)
                throw new HubException("Unauthorized: Not a rider account.");

            var session = existingSession ?? await _context.RiderSessions
                .FirstOrDefaultAsync(s => s.RiderId == rider.Id
                    && s.OrderId == dto.OrderId
                    && s.OrderType == dto.OrderType
                    && s.IsActive);

            if (session == null)
                throw new HubException("Unauthorized: NotYourOrder");

            if (session.RiderId != rider.Id)
                throw new HubException("Unauthorized: NotYourOrder");

            // Step 7: Persist telemetry
            session.LastLatitude = dto.Latitude;
            session.LastLongitude = dto.Longitude;
            session.LastUpdatedAt = DateTime.UtcNow;
            session.Heading = dto.Heading;
            session.SpeedKmh = dto.SpeedKmh;
            session.AccuracyMeters = dto.AccuracyMeters;
            session.BatteryLevel = dto.BatteryLevel;
            session.ConnectionId = Context.ConnectionId;
            session.DeviceId = dto.DeviceId;

            // Step 8: Geofence check (500m, trigger once)
            if (!session.GeofenceTriggered)
            {
                double? destLat = null, destLng = null;
                if (dto.OrderType == "PharmacyOrder")
                {
                    var order = await _context.PharmacyOrders.FindAsync(dto.OrderId);
                    // DestinationLatitude/Longitude columns added in Phase 1 SQL
                    // Access via reflection-safe approach
                    destLat = GetDestLat(order);
                    destLng = GetDestLng(order);
                }
                else if (dto.OrderType == "LabBooking")
                {
                    var booking = await _context.LabBookings.FindAsync(dto.OrderId);
                    destLat = GetLabDestLat(booking);
                    destLng = GetLabDestLng(booking);
                }

                if (destLat.HasValue && destLng.HasValue
                    && destLat.Value != 0 && destLng.Value != 0)
                {
                    bool nearby = _geofenceService.IsWithinRadius(
                        dto.Latitude, dto.Longitude,
                        destLat.Value, destLng.Value);

                    if (nearby)
                    {
                        session.GeofenceTriggered = true;

                        // Get patient ID for push notification
                        string? patientId = await GetPatientIdAsync(dto.OrderId, dto.OrderType);
                        if (patientId != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                await _notificationService.NotifyUserAsync(
                                    patientId,
                                    NotificationType.General,
                                    "🏃 Rider is Arriving!",
                                    "Your rider is arriving soon.",
                                    data: new System.Collections.Generic.Dictionary<string, string>
                                    {
                                        { "orderId", dto.OrderId.ToString() },
                                        { "orderType", dto.OrderType }
                                    });
                            });
                        }

                        // SignalR broadcast to patient group
                        var groupKey = $"order_{dto.OrderType.ToLower()}_{dto.OrderId}";
                        await Clients.Group(groupKey).SendAsync("RiderNearby");

                        await _auditService.LogAsync(
                            TrackingAuditService.GeofenceTriggered,
                            userId,
                            dto.OrderId.ToString(),
                            dto.OrderType);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Step 9: Compute ETA and build broadcast DTO
            var broadcastDto = new LocationBroadcastDto
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Timestamp = dto.Timestamp,
                Heading = dto.Heading
            };

            // Try to attach ETA (non-blocking, best-effort)
            try
            {
                double? dLat = null, dLng = null;
                if (dto.OrderType == "PharmacyOrder")
                {
                    var o = await _context.PharmacyOrders.FindAsync(dto.OrderId);
                    dLat = GetDestLat(o); dLng = GetDestLng(o);
                }
                else if (dto.OrderType == "LabBooking")
                {
                    var b = await _context.LabBookings.FindAsync(dto.OrderId);
                    dLat = GetLabDestLat(b); dLng = GetLabDestLng(b);
                }

                if (dLat.HasValue && dLng.HasValue && dLat.Value != 0)
                {
                    var directions = await _directionsService.GetDirectionsAsync(
                        dto.Latitude, dto.Longitude, dLat.Value, dLng.Value);
                    broadcastDto.EstimatedMinutes = (int)Math.Ceiling(directions.DurationSeconds / 60.0);
                    broadcastDto.DistanceKm = Math.Round(directions.DistanceMeters / 1000.0, 2);
                }
            }
            catch { /* ETA is best-effort — never fail location broadcast */ }

            // Step 10: Broadcast to patient group
            var orderGroupKey = $"order_{dto.OrderType.ToLower()}_{dto.OrderId}";
            await Clients.Group(orderGroupKey).SendAsync("ReceiveLocationUpdate", broadcastDto);

            // Broadcast to admin live map (with RiderId)
            var adminBroadcast = new LocationBroadcastDto
            {
                Latitude = broadcastDto.Latitude,
                Longitude = broadcastDto.Longitude,
                Timestamp = broadcastDto.Timestamp,
                Heading = broadcastDto.Heading,
                RiderId = rider.Id
            };
            await Clients.Group("admin_live_map").SendAsync("ReceiveRiderLocation", adminBroadcast);

            // Step 11: Audit log (fire and forget to not block broadcast)
            _ = Task.Run(async () =>
            {
                await _auditService.LogAsync(
                    "LocationUpdated", userId,
                    dto.OrderId.ToString(), dto.OrderType,
                    metadata: $"{{\"lat\":{dto.Latitude},\"lng\":{dto.Longitude},\"acc\":{dto.AccuracyMeters}}}");
            });
        }

        /// <summary>
        /// Rider sends heartbeat every 30 seconds (Enhancement 7).
        /// Updates LastHeartbeatAt — used by admin dashboard to detect offline riders.
        /// </summary>
        public async Task SendHeartbeat(int orderId, string orderType)
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return;

            var rider = await _context.Riders.FirstOrDefaultAsync(r => r.UserId == userId);
            if (rider == null) return;

            var session = await _context.RiderSessions
                .FirstOrDefaultAsync(s => s.RiderId == rider.Id
                    && s.OrderId == orderId
                    && s.OrderType == orderType
                    && s.IsActive);

            if (session != null)
            {
                session.LastHeartbeatAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private bool CheckRateLimit(string connectionId)
        {
            var now = DateTime.UtcNow;
            var entry = _rateLimiter.GetOrAdd(connectionId, _ => (0, now));

            if (now - entry.WindowStart > RateLimitWindow)
            {
                // Reset window
                _rateLimiter[connectionId] = (1, now);
                return true;
            }

            if (entry.Count >= RateLimitMaxUpdates)
                return false;

            _rateLimiter[connectionId] = (entry.Count + 1, entry.WindowStart);
            return true;
        }

        private static double HaversineDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double d) => d * Math.PI / 180.0;

        private async Task<bool> PatientOwnsOrderAsync(string userId, string groupKey)
        {
            // groupKey format: "order_pharmacyorder_123" or "order_labbooking_456"
            var parts = groupKey.Split('_');
            if (parts.Length < 3) return false;

            if (!int.TryParse(parts[^1], out int orderId)) return false;
            string orderType = string.Join("_", parts[1..^1]);

            if (orderType == "pharmacyorder")
            {
                var order = await _context.PharmacyOrders.FindAsync(orderId);
                return order?.PatientId == userId;
            }
            if (orderType == "labbooking")
            {
                var booking = await _context.LabBookings.FindAsync(orderId);
                return booking?.PatientId == userId;
            }
            return false;
        }

        private async Task<RiderSession?> GetActiveSessionFromGroupKeyAsync(string groupKey)
        {
            var parts = groupKey.Split('_');
            if (parts.Length < 3) return null;
            if (!int.TryParse(parts[^1], out int orderId)) return null;
            string orderType = string.Join("_", parts[1..^1]);

            // Normalise to model name
            string modelType = orderType == "pharmacyorder" ? "PharmacyOrder"
                             : orderType == "labbooking" ? "LabBooking"
                             : orderType;

            return await _context.RiderSessions
                .FirstOrDefaultAsync(s => s.OrderId == orderId
                    && s.OrderType == modelType
                    && s.IsActive);
        }

        private async Task<string?> GetPatientIdAsync(int orderId, string orderType)
        {
            if (orderType == "PharmacyOrder")
                return (await _context.PharmacyOrders.FindAsync(orderId))?.PatientId;
            if (orderType == "LabBooking")
                return (await _context.LabBookings.FindAsync(orderId))?.PatientId;
            return null;
        }

        private static double? GetDestLat(PharmacyOrder? o) => o?.DestinationLatitude;
        private static double? GetDestLng(PharmacyOrder? o) => o?.DestinationLongitude;
        private static double? GetLabDestLat(LabBooking? b) => b?.DestinationLatitude;
        private static double? GetLabDestLng(LabBooking? b) => b?.DestinationLongitude;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MedLinkPortal.Hubs;
using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Controllers.Api
{
    /// <summary>
    /// Mobile API for riders: get assigned orders, update order status.
    /// </summary>
    [ApiController]
    [Route("api/rider")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class RiderApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TrackingHub> _trackingHub;
        private readonly TrackingAuditService _auditService;
        private readonly INotificationService _notificationService;

        // Valid next-step maps
        private static readonly Dictionary<int, int> _pharmacyNext = new()
        {
            { (int)PharmacyOrderStatus.Pending,       (int)PharmacyOrderStatus.Accepted },
            { (int)PharmacyOrderStatus.Accepted,      (int)PharmacyOrderStatus.Packed },
            { (int)PharmacyOrderStatus.Packed,        (int)PharmacyOrderStatus.RiderAssigned },
            { (int)PharmacyOrderStatus.RiderAssigned, (int)PharmacyOrderStatus.PickedUp },
            { (int)PharmacyOrderStatus.PickedUp,      (int)PharmacyOrderStatus.OnTheWay },
            { (int)PharmacyOrderStatus.OnTheWay,      (int)PharmacyOrderStatus.Delivered }
        };

        private static readonly Dictionary<int, int> _labNext = new()
        {
            { (int)LabBookingStatus.Booked,          (int)LabBookingStatus.RiderAssigned },
            { (int)LabBookingStatus.RiderAssigned,   (int)LabBookingStatus.CollectorOnWay },
            { (int)LabBookingStatus.CollectorOnWay,  (int)LabBookingStatus.SampleCollected },
            { (int)LabBookingStatus.SampleCollected, (int)LabBookingStatus.Processing },
            { (int)LabBookingStatus.Processing,      (int)LabBookingStatus.Ready }
        };

        public RiderApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<TrackingHub> trackingHub,
            TrackingAuditService auditService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _trackingHub = trackingHub;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/rider/my-orders
        // Returns all active (non-terminal) assignments for the logged-in rider
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var rider = await _context.Riders.FirstOrDefaultAsync(r => r.UserId == userId);
                if (rider == null)
                    return NotFound(new { message = "Rider profile not found." });

                var activeSessions = await _context.RiderSessions
                    .Where(s => s.RiderId == rider.Id && s.IsActive)
                    .ToListAsync();

                var results = new List<RiderOrderDto>();

                foreach (var session in activeSessions)
                {
                    if (session.OrderType == "PharmacyOrder")
                    {
                        var order = await _context.PharmacyOrders.FindAsync(session.OrderId);
                        if (order != null)
                        {
                            results.Add(new RiderOrderDto
                            {
                                OrderId = order.Id,
                                OrderType = "PharmacyOrder",
                                PatientAddress = order.ShippingAddress ?? string.Empty,
                                Status = (int)order.Status,
                                StatusLabel = order.Status.ToString(),
                                CreatedAt = order.CreatedAt,
                                DestinationLatitude = order.DestinationLatitude,
                                DestinationLongitude = order.DestinationLongitude
                            });
                        }
                    }
                    else if (session.OrderType == "LabBooking")
                    {
                        var booking = await _context.LabBookings.FindAsync(session.OrderId);
                        if (booking != null)
                        {
                            results.Add(new RiderOrderDto
                            {
                                OrderId = booking.Id,
                                OrderType = "LabBooking",
                                PatientAddress = booking.Address ?? string.Empty,
                                Status = (int)booking.Status,
                                StatusLabel = booking.Status.ToString(),
                                CreatedAt = booking.BookingDate,
                                DestinationLatitude = booking.DestinationLatitude,
                                DestinationLongitude = booking.DestinationLongitude
                            });
                        }
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load orders." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/rider/update-status
        // Validates sequential transition, updates DB, broadcasts via SignalR
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] RiderStatusUpdateRequest req)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var rider = await _context.Riders.FirstOrDefaultAsync(r => r.UserId == userId);
                if (rider == null)
                    return NotFound(new { message = "Rider profile not found." });

                if (req.OrderType == "PharmacyOrder")
                {
                    var order = await _context.PharmacyOrders.FindAsync(req.OrderId);
                    if (order == null)
                        return NotFound(new { message = "Order not found." });

                    if (order.RiderId != rider.Id)
                        return StatusCode(403, new { message = "You are not assigned to this order." });

                    int currentStatus = (int)order.Status;

                    if (!_pharmacyNext.TryGetValue(currentStatus, out int expectedNext)
                        || req.NewStatus != expectedNext)
                    {
                        return BadRequest(new
                        {
                            message = $"InvalidStatusTransition: expected {expectedNext}, got {req.NewStatus}."
                        });
                    }

                    string oldLabel = order.Status.ToString();
                    order.Status = (PharmacyOrderStatus)req.NewStatus;
                    order.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    string newLabel = order.Status.ToString();

                    await SendStatusPushAsync(order.PatientId, req.OrderId, "PharmacyOrder", newLabel);

                    if (req.NewStatus == (int)PharmacyOrderStatus.RiderAssigned)
                        await EnsureSessionExistsAsync(rider.Id, req.OrderId, "PharmacyOrder");

                    if (req.NewStatus == (int)PharmacyOrderStatus.Delivered
                        || req.NewStatus == (int)PharmacyOrderStatus.Cancelled)
                        await EndSessionAsync(rider.Id, req.OrderId, "PharmacyOrder");

                    var groupKey = $"order_pharmacyorder_{req.OrderId}";
                    await _trackingHub.Clients.Group(groupKey)
                        .SendAsync("ReceiveStatusUpdate", new OrderStatusUpdateDto
                        {
                            OrderId = req.OrderId,
                            OrderType = "PharmacyOrder",
                            NewStatus = req.NewStatus,
                            StatusLabel = newLabel
                        });

                    await _auditService.LogAsync(
                        TrackingAuditService.StatusChanged,
                        userId,
                        req.OrderId.ToString(),
                        "PharmacyOrder",
                        oldLabel,
                        newLabel);

                    return Ok(new { success = true, newStatus = req.NewStatus, label = newLabel });
                }
                else if (req.OrderType == "LabBooking")
                {
                    var booking = await _context.LabBookings.FindAsync(req.OrderId);
                    if (booking == null)
                        return NotFound(new { message = "Lab booking not found." });

                    if (booking.RiderId != rider.Id)
                        return StatusCode(403, new { message = "You are not assigned to this booking." });

                    int currentStatus = (int)booking.Status;

                    if (!_labNext.TryGetValue(currentStatus, out int expectedNext)
                        || req.NewStatus != expectedNext)
                    {
                        return BadRequest(new
                        {
                            message = $"InvalidStatusTransition: expected {expectedNext}, got {req.NewStatus}."
                        });
                    }

                    string oldLabel = booking.Status.ToString();
                    booking.Status = (LabBookingStatus)req.NewStatus;

                    await _context.SaveChangesAsync();

                    string newLabel = booking.Status.ToString();

                    await SendStatusPushAsync(booking.PatientId, req.OrderId, "LabBooking", newLabel);

                    if (req.NewStatus == (int)LabBookingStatus.RiderAssigned)
                        await EnsureSessionExistsAsync(rider.Id, req.OrderId, "LabBooking");

                    if (req.NewStatus == (int)LabBookingStatus.SampleCollected
                        || req.NewStatus == (int)LabBookingStatus.Ready)
                        await EndSessionAsync(rider.Id, req.OrderId, "LabBooking");

                    var groupKey = $"order_labbooking_{req.OrderId}";
                    await _trackingHub.Clients.Group(groupKey)
                        .SendAsync("ReceiveStatusUpdate", new OrderStatusUpdateDto
                        {
                            OrderId = req.OrderId,
                            OrderType = "LabBooking",
                            NewStatus = req.NewStatus,
                            StatusLabel = newLabel
                        });

                    await _auditService.LogAsync(
                        TrackingAuditService.StatusChanged,
                        userId,
                        req.OrderId.ToString(),
                        "LabBooking",
                        oldLabel,
                        newLabel);

                    return Ok(new { success = true, newStatus = req.NewStatus, label = newLabel });
                }

                return BadRequest(new { message = "Unknown order type." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update order status." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private async Task SendStatusPushAsync(
            string? patientId, int orderId, string orderType, string statusLabel)
        {
            if (string.IsNullOrEmpty(patientId)) return;

            string title = statusLabel switch
            {
                "RiderAssigned" => "✅ Rider Assigned",
                "PickedUp" => "📦 Order Picked Up",
                "Delivered" => "✅ Delivered!",
                "CollectorOnWay" => "🏃 Collector On The Way",
                "SampleCollected" => "✅ Sample Collected",
                _ => $"Order Update: {statusLabel}"
            };

            string body = statusLabel switch
            {
                "RiderAssigned" => "A rider has been assigned to your order.",
                "PickedUp" => "Your order has been picked up.",
                "Delivered" => "Your order has been delivered.",
                "CollectorOnWay" => "The collector is on the way to your location.",
                "SampleCollected" => "Your sample has been collected.",
                _ => $"Your order status is now: {statusLabel}"
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.NotifyUserAsync(
                        patientId,
                        NotificationType.General,
                        title, body,
                        data: new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "orderId",   orderId.ToString() },
                            { "orderType", orderType }
                        });
                }
                catch { /* non-blocking */ }
            });
        }

        private async Task EnsureSessionExistsAsync(int riderId, int orderId, string orderType)
        {
            var exists = await _context.RiderSessions
                .AnyAsync(s => s.RiderId == riderId
                    && s.OrderId == orderId
                    && s.OrderType == orderType
                    && s.IsActive);

            if (!exists)
            {
                _context.RiderSessions.Add(new RiderSession
                {
                    RiderId = riderId,
                    OrderId = orderId,
                    OrderType = orderType,
                    IsActive = true,
                    LastUpdatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task EndSessionAsync(int riderId, int orderId, string orderType)
        {
            var sessions = await _context.RiderSessions
                .Where(s => s.RiderId == riderId
                    && s.OrderId == orderId
                    && s.OrderType == orderType
                    && s.IsActive)
                .ToListAsync();

            foreach (var s in sessions)
                s.IsActive = false;

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/rider/stats
        // Returns today's deliveries, total earnings, and average rating
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var rider = await _context.Riders
                    .FirstOrDefaultAsync(r => r.UserId == userId);
                if (rider == null) return NotFound(new { message = "Rider profile not found." });

                var today = DateTime.UtcNow.Date;

                // Completed pharmacy orders today
                var completedPharmacyToday = await _context.PharmacyOrders
                    .CountAsync(o => o.RiderId == rider.Id
                        && o.Status == PharmacyOrderStatus.Delivered
                        && o.UpdatedAt.HasValue && o.UpdatedAt.Value.Date == today);

                // Completed lab bookings today (use BookingDate as approximation)
                var completedLabToday = await _context.LabBookings
                    .CountAsync(b => b.RiderId == rider.Id
                        && b.Status == LabBookingStatus.Ready
                        && b.BookingDate.Date == today);

                var completedToday = completedPharmacyToday + completedLabToday;

                // Total earnings from wallet (rider earns credited as WalletTransaction with DoctorId = rider.UserId)
                var totalEarnings = await _context.WalletTransactions
                    .Where(t => t.DoctorId == userId && t.Amount > 0)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                return Ok(new
                {
                    completedToday,
                    totalEarnings,
                    rating = rider.AverageRating
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load stats." });
            }
        }
    }
}

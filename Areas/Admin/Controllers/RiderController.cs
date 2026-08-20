using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Hubs;
using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RiderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TrackingAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<TrackingHub> _trackingHub;

        public RiderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TrackingAuditService auditService,
            INotificationService notificationService,
            IHubContext<TrackingHub> trackingHub)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _notificationService = notificationService;
            _trackingHub = trackingHub;
        }

        // GET /Admin/Rider
        public async Task<IActionResult> Index()
        {
            var riders = await _context.Riders
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Build status map: Available / On Delivery / Offline
            var activeSessions = await _context.RiderSessions
                .Where(s => s.IsActive)
                .Select(s => s.RiderId)
                .Distinct()
                .ToListAsync();

            ViewBag.ActiveRiderIds = activeSessions;
            return View(riders);
        }

        // GET /Admin/Rider/Create
        public IActionResult Create()
        {
            return View(new CreateRiderViewModel());
        }

        // POST /Admin/Rider/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRiderViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Duplicate phone check
            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.Phone);
            if (existingUser != null)
            {
                ModelState.AddModelError("Phone", "Phone number already in use.");
                return View(model);
            }

            // Create ApplicationUser with Rider role
            var email = $"rider_{model.Phone.Replace("+", "").Replace(" ", "")}@medlink.com";
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.Phone,
                EmailConfirmed = true,
                ApprovalStatus = "Approved"
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync("Rider"))
                await _roleManager.CreateAsync(new IdentityRole("Rider"));

            await _userManager.AddToRoleAsync(user, "Rider");

            // Create Rider profile
            var rider = new Rider
            {
                UserId = user.Id,
                VehicleType = model.VehicleType,
                VehicleNumber = model.VehicleNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Riders.Add(rider);
            await _context.SaveChangesAsync();

            var adminId = _userManager.GetUserId(User) ?? "system";
            await _auditService.LogAsync(
                TrackingAuditService.RiderCreated,
                adminId,
                rider.Id.ToString(),
                "Rider",
                newValue: $"{model.FirstName} {model.LastName}");

            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/Rider/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] int riderId)
        {
            var rider = await _context.Riders.FindAsync(riderId);
            if (rider == null) return NotFound();

            rider.IsActive = !rider.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { isActive = rider.IsActive });
        }

        // POST /Admin/Rider/AssignRider
        [HttpPost]
        public async Task<IActionResult> AssignRider([FromBody] AssignRiderRequest req)
        {
            var rider = await _context.Riders
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == req.RiderId);

            if (rider == null)
                return NotFound(new { message = "Rider not found." });

            if (!rider.IsActive)
                return BadRequest(new { message = "Rider is deactivated." });

            var adminId = _userManager.GetUserId(User) ?? "system";

            if (req.OrderType == "PharmacyOrder")
            {
                var order = await _context.PharmacyOrders.FindAsync(req.OrderId);
                if (order == null) return NotFound(new { message = "Order not found." });

                if (order.Status != PharmacyOrderStatus.Accepted
                    && order.Status != PharmacyOrderStatus.Packed)
                    return Conflict(new { message = "Order must be Accepted or Packed to assign a rider." });

                order.RiderId = rider.Id;
                order.Status = PharmacyOrderStatus.RiderAssigned;
                order.UpdatedAt = DateTime.UtcNow;

                // Create RiderSession
                _context.RiderSessions.Add(new RiderSession
                {
                    RiderId = rider.Id,
                    OrderId = req.OrderId,
                    OrderType = "PharmacyOrder",
                    IsActive = true,
                    LastUpdatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Push notification to patient
                await SendRiderAssignedPushAsync(order.PatientId, rider, req.OrderId, "PharmacyOrder");

                await _auditService.LogAsync(
                    TrackingAuditService.RiderAssigned,
                    adminId,
                    req.OrderId.ToString(),
                    "PharmacyOrder",
                    newValue: $"RiderId={rider.Id}");

                return Ok(new { success = true });
            }

            if (req.OrderType == "LabBooking")
            {
                var booking = await _context.LabBookings.FindAsync(req.OrderId);
                if (booking == null) return NotFound(new { message = "Booking not found." });

                if (!booking.IsHomeCollection)
                    return BadRequest(new { message = "Rider assignment only for home collection bookings." });

                if (booking.Status != LabBookingStatus.Booked)
                    return Conflict(new { message = "Booking must be in Booked status to assign a rider." });

                booking.RiderId = rider.Id;
                booking.Status = LabBookingStatus.RiderAssigned;

                _context.RiderSessions.Add(new RiderSession
                {
                    RiderId = rider.Id,
                    OrderId = req.OrderId,
                    OrderType = "LabBooking",
                    IsActive = true,
                    LastUpdatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                await SendRiderAssignedPushAsync(booking.PatientId, rider, req.OrderId, "LabBooking");

                await _auditService.LogAsync(
                    TrackingAuditService.RiderAssigned,
                    adminId,
                    req.OrderId.ToString(),
                    "LabBooking",
                    newValue: $"RiderId={rider.Id}");

                return Ok(new { success = true });
            }

            return BadRequest(new { message = "Unknown order type." });
        }

        // GET /Admin/Rider/TrackingDashboard
        public IActionResult TrackingDashboard()
        {
            return View();
        }

        // GET /Admin/Rider/GetActiveSessions  (JSON for 30s poll)
        [HttpGet]
        public async Task<IActionResult> GetActiveSessions()
        {
            var sessions = await _context.RiderSessions
                .Include(s => s.RiderProfile)
                    .ThenInclude(r => r!.User)
                .Where(s => s.IsActive)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var dtos = new List<ActiveSessionDto>();

            foreach (var s in sessions)
            {
                string statusLabel = "Active";
                if (s.OrderType == "PharmacyOrder")
                {
                    var o = await _context.PharmacyOrders.FindAsync(s.OrderId);
                    statusLabel = o?.Status.ToString() ?? "Unknown";
                }
                else if (s.OrderType == "LabBooking")
                {
                    var b = await _context.LabBookings.FindAsync(s.OrderId);
                    statusLabel = b?.Status.ToString() ?? "Unknown";
                }

                var riderName = s.RiderProfile?.User != null
                    ? $"{s.RiderProfile.User.FirstName} {s.RiderProfile.User.LastName}".Trim()
                    : "Unknown";

                dtos.Add(new ActiveSessionDto
                {
                    SessionId = s.Id,
                    RiderId = s.RiderId,
                    RiderName = riderName,
                    OrderId = s.OrderId,
                    OrderType = s.OrderType,
                    StatusLabel = statusLabel,
                    ElapsedSecondsSinceUpdate = (int)(now - s.LastUpdatedAt).TotalSeconds,
                    IsHeartbeatStale = s.LastHeartbeatAt == null
                                              || (now - s.LastHeartbeatAt.Value).TotalMinutes >= 2
                });
            }

            return Ok(dtos);
        }

        // GET /Admin/Rider/LiveMapDashboard
        public IActionResult LiveMapDashboard()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        private async Task SendRiderAssignedPushAsync(
            string? patientId, Rider rider, int orderId, string orderType)
        {
            if (string.IsNullOrEmpty(patientId)) return;

            var riderName = rider.User != null
                ? $"{rider.User.FirstName} {rider.User.LastName}".Trim()
                : "Your rider";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.NotifyUserAsync(
                        patientId,
                        NotificationType.General,
                        "✅ Rider Assigned",
                        $"{riderName} has been assigned to your order.",
                        data: new Dictionary<string, string>
                        {
                            { "orderId",   orderId.ToString() },
                            { "orderType", orderType }
                        });
                }
                catch { /* non-blocking */ }
            });
        }
    }
}

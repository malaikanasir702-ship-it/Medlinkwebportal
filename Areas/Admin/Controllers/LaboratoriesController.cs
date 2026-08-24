using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LaboratoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public LaboratoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var labIds = await _context.Laboratories.Select(l => l.Id).ToListAsync();

            // Load booking stats per lab
            var bookingStats = await _context.LabBookings
                .GroupBy(b => b.LaboratoryId)
                .Select(g => new
                {
                    LabId = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(b => b.Status == LabBookingStatus.Booked),
                    Completed = g.Count(b => b.Status == LabBookingStatus.Ready)
                })
                .ToListAsync();

            // Revenue: sum of prices of booking items for completed bookings
            var revenueStats = await _context.LabBookings
                .Where(b => b.Status == LabBookingStatus.Ready)
                .Join(_context.LabBookingItems, b => b.Id, i => i.LabBookingId, (b, i) => new { b.LaboratoryId, i.MedicalTestId })
                .Join(_context.MedicalTests, bi => bi.MedicalTestId, t => t.Id, (bi, t) => new { bi.LaboratoryId, t.Price })
                .GroupBy(x => x.LaboratoryId)
                .Select(g => new { LabId = g.Key, Revenue = g.Sum(x => x.Price) })
                .ToListAsync();

            var labs = await _context.Laboratories
                .Include(l => l.City)
                .ToListAsync();

            var result = labs.Select(l =>
            {
                var stats = bookingStats.FirstOrDefault(s => s.LabId == l.Id);
                var rev = revenueStats.FirstOrDefault(r => r.LabId == l.Id);
                return new LabCompanyViewModel
                {
                    Id = l.Id,
                    Name = l.Name,
                    Address = l.Address,
                    PhoneNumber = l.PhoneNumber,
                    CityName = l.City?.Name ?? "",
                    TestCount = _context.MedicalTests.Count(t => t.LaboratoryId == l.Id),
                    AdminEmail = _context.Users.Where(u => u.LaboratoryId == l.Id).Select(u => u.Email).FirstOrDefault() ?? "No Admin Assigned",
                    TotalBookings = stats?.Total ?? 0,
                    PendingBookings = stats?.Pending ?? 0,
                    CompletedBookings = stats?.Completed ?? 0,
                    TotalRevenue = rev?.Revenue ?? 0
                };
            }).ToList();

            ViewBag.Cities = await _context.Cities.ToListAsync();
            return View(result);
        }

        // GET: /Admin/Laboratories/Bookings?labId=X
        public async Task<IActionResult> Bookings(int? labId)
        {
            var query = _context.LabBookings
                .Include(b => b.Laboratory)
                .Include(b => b.Patient)
                .Include(b => b.BookingItems)
                    .ThenInclude(i => i.MedicalTest)
                .Include(b => b.TestResults)
                .AsQueryable();

            if (labId.HasValue)
            {
                query = query.Where(b => b.LaboratoryId == labId.Value);
            }

            var bookings = await query.OrderByDescending(b => b.BookingDate).ToListAsync();

            // Load rider info
            var riderIds = bookings.Where(b => b.RiderId.HasValue).Select(b => b.RiderId!.Value).Distinct().ToList();
            var riders = await _context.Riders
                .Include(r => r.User)
                .Where(r => riderIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r);

            var viewModels = bookings.Select(b =>
            {
                var testsSummary = b.BookingItems != null && b.BookingItems.Any()
                    ? string.Join(", ", b.BookingItems.Select(i => i.MedicalTest?.Name ?? "Unknown"))
                    : "No tests";

                string? riderName = null;
                if (b.RiderId.HasValue && riders.TryGetValue(b.RiderId.Value, out var rider))
                {
                    riderName = rider.User != null
                        ? $"{rider.User.FirstName} {rider.User.LastName}".Trim()
                        : null;
                }

                // Calculate total: sum of prices of booked tests
                decimal totalAmount = b.BookingItems != null
                    ? b.BookingItems.Sum(i => i.MedicalTest?.Price ?? 0)
                    : 0;

                return new LabBookingAdminViewModel
                {
                    BookingId = b.Id,
                    LabName = b.Laboratory?.Name ?? "Unknown",
                    PatientName = b.PatientName ?? (b.Patient != null ? $"{b.Patient.FirstName} {b.Patient.LastName}".Trim() : "Unknown"),
                    PhoneNumber = b.PhoneNumber ?? "",
                    TestsSummary = testsSummary,
                    TotalAmount = totalAmount,
                    IsHomeCollection = b.IsHomeCollection,
                    Status = b.Status.ToString(),
                    StatusCode = (int)b.Status,
                    PreferredDate = b.PreferredDate,
                    BookingDate = b.BookingDate,
                    RiderName = riderName,
                    HasReport = b.TestResults != null && b.TestResults.Any()
                };
            }).ToList();

            if (labId.HasValue)
            {
                var lab = await _context.Laboratories.FindAsync(labId.Value);
                ViewBag.LabName = lab?.Name;
            }
            else
            {
                ViewBag.LabName = null;
            }

            return View(viewModels);
        }

        // POST: /Admin/Laboratories/UpdateBookingStatus
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, int status)
        {
            var booking = await _context.LabBookings.FindAsync(bookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            booking.Status = (LabBookingStatus)status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LabCompanyViewModel model)
        {
            if (model == null) return BadRequest(new { message = "Invalid data" });

            // 1. Create Laboratory
            var lab = new Laboratory
            {
                Name = model.Name,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                CityId = model.CityId,
                OpenTime = "09:00 AM",
                CloseTime = "09:00 PM",
                Rating = 5.0,
                HomeCollectionAvailable = true
            };

            _context.Laboratories.Add(lab);
            await _context.SaveChangesAsync();

            // 2. Create LabAdmin User if Email/Password provided
            if (!string.IsNullOrEmpty(model.AdminEmail) && !string.IsNullOrEmpty(model.AdminPassword))
            {
                var user = new ApplicationUser
                {
                    UserName = model.AdminEmail,
                    Email = model.AdminEmail,
                    FirstName = model.Name,
                    LastName = "Admin",
                    EmailConfirmed = true,
                    LaboratoryId = lab.Id,
                    ApprovalStatus = "Approved"
                };

                var userResult = await _userManager.CreateAsync(user, model.AdminPassword);
                if (userResult.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("LabAdmin"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("LabAdmin"));
                    }
                    await _userManager.AddToRoleAsync(user, "LabAdmin");
                }
                else
                {
                    // If user creation fails, we still have the lab. Maybe return warning.
                    return BadRequest(new { message = "Laboratory created, but admin user creation failed.", errors = userResult.Errors.Select(e => e.Description) });
                }
            }

            return Ok(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var lab = await _context.Laboratories.FindAsync(id);
            if (lab == null) return NotFound();

            // Note: In a real app, you'd handle cascading deletes or prevent delete if dependencies exist.
            _context.Laboratories.Remove(lab);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class LabCompanyViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; } = "";
        public int TestCount { get; set; }
        public string AdminEmail { get; set; } = "";
        public string? AdminPassword { get; set; }
        // Booking stats
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LabBookingAdminViewModel
    {
        public int BookingId { get; set; }
        public string LabName { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string TestsSummary { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public bool IsHomeCollection { get; set; }
        public string Status { get; set; } = "";
        public int StatusCode { get; set; }
        public DateTime PreferredDate { get; set; }
        public DateTime BookingDate { get; set; }
        public string? RiderName { get; set; }
        public bool HasReport { get; set; }
    }
}

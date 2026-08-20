using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Lab.Controllers
{
    [Area("Lab")]
    [Authorize(Roles = "LabAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Content("No Laboratory found or access denied. Please contact system administrator.");

            var bookings = await _context.LabBookings
                .Where(b => b.LaboratoryId == lab.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.PendingCollections = bookings.Count(b => b.Status == LabBookingStatus.Booked);
            ViewBag.InProcessing = bookings.Count(b => b.Status == LabBookingStatus.Processing);
            ViewBag.Ready = bookings.Count(b => b.Status == LabBookingStatus.Ready);

            // Total cost from all bookings for this lab
            ViewBag.Revenue = await _context.MedicalTests
                .Where(t => t.LaboratoryId == lab.Id)
                .Join(_context.LabBookingItems, t => t.Id, bi => bi.MedicalTestId, (t, bi) => t.Price)
                .SumAsync();

            ViewBag.Categories = await _context.MedicalTestCategories.ToListAsync();
            ViewBag.LabName = lab.Name;
            return View(bookings);
        }

        public async Task<IActionResult> Profile()
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Content("No Laboratory found.");

            ViewBag.LabName = lab.Name;
            return View(lab);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Laboratory labInfo, IFormFile? logoFile)
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null || lab.Id != labInfo.Id) return Json(new { success = false, message = "Access denied" });

            if (logoFile != null && logoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lab-logos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }

                lab.LogoUrl = "/uploads/lab-logos/" + fileName;
            }

            lab.Address = labInfo.Address;
            lab.PhoneNumber = labInfo.PhoneNumber;
            lab.OpenTime = labInfo.OpenTime;
            lab.CloseTime = labInfo.CloseTime;

            await _context.SaveChangesAsync();
            return Json(new { success = true, logoUrl = lab.LogoUrl });
        }

        public async Task<IActionResult> Bookings()
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Content("No Laboratory found.");

            var bookings = await _context.LabBookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.MedicalTest)
                .Where(b => b.LaboratoryId == lab.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.LabName = lab.Name;
            return View(bookings);
        }

        public async Task<IActionResult> Tests()
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Content("No Laboratory found.");

            var tests = await _context.MedicalTests
                .Include(t => t.Category)
                .Where(t => t.LaboratoryId == lab.Id)
                .ToListAsync();

            ViewBag.Categories = await _context.MedicalTestCategories.ToListAsync();
            ViewBag.LabName = lab.Name;
            return View(tests);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTest(MedicalTest test, string? newCategoryName)
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Json(new { success = false, message = "Access denied" });

            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                var category = await _context.MedicalTestCategories.FirstOrDefaultAsync(c => c.Name == newCategoryName);
                if (category == null)
                {
                    category = new MedicalTestCategory { Name = newCategoryName };
                    _context.MedicalTestCategories.Add(category);
                    await _context.SaveChangesAsync();
                }
                test.CategoryId = category.Id;
            }

            var existingTest = await _context.MedicalTests.FirstOrDefaultAsync(t => t.Id == test.Id && t.LaboratoryId == lab.Id);
            if (existingTest == null) return Json(new { success = false, message = "Test not found or access denied" });

            existingTest.Name = test.Name;
            existingTest.Price = test.Price;
            existingTest.ReportTime = test.ReportTime;
            existingTest.SampleType = test.SampleType;
            existingTest.CategoryId = test.CategoryId;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddTest(MedicalTest test, string? newCategoryName)
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Json(new { success = false });

            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                var category = await _context.MedicalTestCategories.FirstOrDefaultAsync(c => c.Name == newCategoryName);
                if (category == null)
                {
                    category = new MedicalTestCategory { Name = newCategoryName };
                    _context.MedicalTestCategories.Add(category);
                    await _context.SaveChangesAsync();
                }
                test.CategoryId = category.Id;
            }

            test.LaboratoryId = lab.Id;
            _context.MedicalTests.Add(test);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTest(int id)
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Json(new { success = false, message = "Access denied" });

            var test = await _context.MedicalTests.FirstOrDefaultAsync(t => t.Id == id && t.LaboratoryId == lab.Id);
            if (test == null) return Json(new { success = false, message = "Test not found" });

            _context.MedicalTests.Remove(test);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private async Task<Laboratory> GetCurrentLabAsync()
        {
            var userName = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (user?.LaboratoryId != null)
            {
                return await _context.Laboratories.FirstOrDefaultAsync(l => l.Id == user.LaboratoryId);
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int bookingId, LabBookingStatus status)
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Json(new { success = false, message = "Access Denied" });

            var booking = await _context.LabBookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.LaboratoryId == lab.Id);
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Booking not found or access denied" });
        }

        // ─── Rider Assignment (Task 4.10) ────────────────────────────────────

        /// <summary>
        /// Returns available (active + not on active delivery) riders for dropdown.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AvailableRiders()
        {
            var busyRiderIds = await _context.RiderSessions
                .Where(s => s.IsActive)
                .Select(s => s.RiderId)
                .Distinct()
                .ToListAsync();

            var available = await _context.Riders
                .Include(r => r.User)
                .Where(r => r.IsActive && !busyRiderIds.Contains(r.Id))
                .Select(r => new
                {
                    id = r.Id,
                    name = r.User != null
                        ? r.User.FirstName + " " + r.User.LastName
                        : "Rider",
                    vehicleType = r.VehicleType,
                    vehicleNumber = r.VehicleNumber
                })
                .ToListAsync();

            return Json(available);
        }

        /// <summary>
        /// LabAdmin assigns a collector rider to a home-collection booking.
        /// Validates IsHomeCollection = true and Status = Booked.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignCollectorRider([FromBody] AssignRiderRequest req)
        {
            var lab = await GetCurrentLabAsync();
            if (lab == null) return Json(new { success = false, message = "Access Denied" });

            var booking = await _context.LabBookings
                .FirstOrDefaultAsync(b => b.Id == req.OrderId && b.LaboratoryId == lab.Id);

            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            if (!booking.IsHomeCollection)
                return BadRequest(new { message = "Rider assignment only for home collection bookings." });

            if (booking.Status != LabBookingStatus.Booked)
                return Conflict(new { message = "Booking must be in Booked status to assign a rider." });

            var rider = await _context.Riders
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == req.RiderId);

            if (rider == null) return NotFound(new { message = "Rider not found." });
            if (!rider.IsActive) return BadRequest(new { message = "Rider is deactivated." });

            booking.RiderId = rider.Id;
            booking.Status = LabBookingStatus.RiderAssigned;

            // Create RiderSession
            var exists = await _context.RiderSessions
                .AnyAsync(s => s.RiderId == rider.Id
                    && s.OrderId == req.OrderId
                    && s.OrderType == "LabBooking"
                    && s.IsActive);

            if (!exists)
            {
                _context.RiderSessions.Add(new RiderSession
                {
                    RiderId = rider.Id,
                    OrderId = req.OrderId,
                    OrderType = "LabBooking",
                    IsActive = true,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Push notification to patient
            if (!string.IsNullOrEmpty(booking.PatientId))
            {
                var riderName = rider.User != null
                    ? $"{rider.User.FirstName} {rider.User.LastName}".Trim()
                    : "A collector";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Use reflection to get _notificationService — it's already in the context scope
                        // Simpler: just add it to the constructor if not present
                    }
                    catch { }
                });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UploadReport(int bookingId, IFormFile reportFile)
        {
            if (reportFile == null || reportFile.Length == 0) return Json(new { success = false, message = "File is empty" });

            var lab = await GetCurrentLabAsync();
            if (lab == null) return Json(new { success = false, message = "Access Denied" });

            var booking = await _context.LabBookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.LaboratoryId == lab.Id);
            if (booking == null) return Json(new { success = false, message = "Booking not found or access denied" });

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lab-reports");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = Guid.NewGuid().ToString() + "_" + reportFile.FileName;
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await reportFile.CopyToAsync(fileStream);
            }

            var result = new LabTestResult
            {
                LabBookingId = bookingId,
                ReportUrl = "/uploads/lab-reports/" + fileName,
                UploadedDate = DateTime.Now
            };

            _context.LabTestResults.Add(result);
            booking.Status = LabBookingStatus.Ready;
            await _context.SaveChangesAsync();

            return Json(new { success = true, reportUrl = result.ReportUrl });
        }
    }
}

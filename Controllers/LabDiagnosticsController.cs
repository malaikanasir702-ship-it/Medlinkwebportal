using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Controllers
{
    public class LabDiagnosticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MedLinkPortal.Services.INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public LabDiagnosticsController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            MedLinkPortal.Services.INotificationService notificationService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        private async Task<PatientDashboardModel> GetBaseModelAsync(string activeTab = "lab-diagnostics")
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            return new PatientDashboardModel
            {
                IsLoading = false,
                ActiveTab = activeTab,
                PatientName = user?.FirstName ?? User.Identity?.Name?.Split('@')[0] ?? "Alex",
                PatientId = userId,
                ProfileImage = user?.ProfileImage ?? "https://picsum.photos/seed/patient/100/100",
                Notifications = await _notificationService.GetUserNotificationsAsync(userId) ?? new List<Notification>(),
                VapidPublicKey = _configuration["Vapid:PublicKey"]
            };
        }

        public async Task<IActionResult> Index()
        {
            var cities = await _context.Cities
                .Select(c => new CityWithStats
                {
                    Id = c.Id,
                    Name = c.Name,
                    LabCount = _context.Laboratories.Count(l => l.CityId == c.Id)
                })
                .ToListAsync();

            var model = await GetBaseModelAsync();
            ViewBag.Cities = cities;
            return View(model);
        }

        public class CityWithStats
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int LabCount { get; set; }
        }

        public async Task<IActionResult> Labs(int cityId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.CityId == cityId)
                .ToListAsync();
            
            var city = await _context.Cities.FindAsync(cityId);
            ViewBag.CityName = city?.Name;
            ViewBag.CityId = cityId;

            var model = await GetBaseModelAsync();
            model.Laboratories = labs;
            return View(model);
        }

        public async Task<IActionResult> Tests(int labId)
        {
            var lab = await _context.Laboratories
                .Include(l => l.MedicalTests)
                .ThenInclude(t => t.Category)
                .FirstOrDefaultAsync(l => l.Id == labId);

            if (lab == null) return NotFound();

            var categories = await _context.MedicalTestCategories
                .Include(c => c.MedicalTests.Where(t => t.LaboratoryId == labId))
                .ToListAsync();

            ViewBag.Lab = lab;
            
            var model = await GetBaseModelAsync();
            model.MedicalTestCategories = categories;
            model.SelectedLaboratory = lab;
            return View(model);
        }

        public async Task<IActionResult> Booking(int labId, string testIds)
        {
            var testIdList = testIds.Split(',').Select(int.Parse).ToList();
            var tests = await _context.MedicalTests
                .Include(t => t.Laboratory)
                .Where(t => testIdList.Contains(t.Id))
                .ToListAsync();

            ViewBag.LaboratoryId = labId;
            ViewBag.Tests = tests;
            ViewBag.Total = tests.Sum(t => t.Price);

            var model = await GetBaseModelAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(LabBooking booking, string testIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            booking.PatientId = userId;
            booking.BookingDate = DateTime.Now;
            booking.Status = LabBookingStatus.Booked;

            _context.LabBookings.Add(booking);
            await _context.SaveChangesAsync();

            var testIdList = testIds.Split(',').Select(int.Parse).ToList();
            foreach (var testId in testIdList)
            {
                _context.LabBookingItems.Add(new LabBookingItem
                {
                    LabBookingId = booking.Id,
                    MedicalTestId = testId
                });
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("Tracking", new { bookingId = booking.Id });
        }

        public async Task<IActionResult> Tracking(int bookingId)
        {
            var booking = await _context.LabBookings
                .Include(b => b.Laboratory)
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.MedicalTest)
                .Include(b => b.TestResults)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            var model = await GetBaseModelAsync();
            ViewBag.Booking = booking;
            return View(model);
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.LabBookings
                .Include(b => b.Laboratory)
                .Where(b => b.PatientId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var model = await GetBaseModelAsync();
            ViewBag.Bookings = bookings;
            return View(model);
        }
    }
}

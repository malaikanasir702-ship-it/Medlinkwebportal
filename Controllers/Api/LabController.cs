using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/lab")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lab")]
    public class LabController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LabController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var stats = new
                {
                    TodayBookings = await _context.LabBookings.CountAsync(b => b.BookingDate.Date == today),
                    PendingResults = await _context.LabBookings.CountAsync(b => b.Status == LabBookingStatus.SampleCollected),
                    TotalTests = await _context.MedicalTests.CountAsync()
                };

                var recentBookings = await _context.LabBookings
                    .Include(b => b.Patient)
                    .Include(b => b.Laboratory)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .Select(b => new {
                        b.Id,
                        PatientName = (b.Patient != null ? b.Patient.FirstName + " " + b.Patient.LastName : "Patient"),
                        LabName = b.Laboratory.Name,
                        Status = b.Status.ToString(),
                        Date = b.BookingDate
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Stats = stats,
                    RecentBookings = recentBookings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load lab dashboard." });
            }
        }

        [HttpGet("tests")]
        public async Task<IActionResult> GetTests()
        {
            try
            {
                var tests = await _context.MedicalTests
                    .Include(t => t.Laboratory)
                    .OrderBy(t => t.Name)
                    .Select(t => new {
                        t.Id,
                        t.Name,
                        LabName = t.Laboratory.Name,
                        t.Price
                    })
                    .ToListAsync();
                return Ok(tests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load tests." });
            }
        }
    }
}

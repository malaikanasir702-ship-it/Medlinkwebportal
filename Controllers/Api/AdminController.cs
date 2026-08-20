using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var today = DateTime.Today;

                // 1. Global Stats
                var stats = new
                {
                    TotalPatients = await _context.Users.CountAsync(),
                    TotalDoctors = await _context.Doctors.CountAsync(),
                    CriticalCases = await _context.AdminPatients.CountAsync(p => p.Status == "CRITICAL"),
                    PendingWithdrawals = await _context.WalletTransactions
                        .CountAsync(t => t.TransactionType == "WITHDRAWAL" && (t.Status == "Pending" || t.Status == "pending"))
                };

                // 2. Triage Queue (Today's active appointments)
                var triageQueue = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.AppointmentDate >= today && a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate)
                    .Take(10)
                    .Select(a => new {
                        a.Id,
                        PatientName = a.PatientName,
                        Specialty = a.Doctor != null ? (a.Doctor.Specialty ?? "General") : "General",
                        Time = a.TimeSlot,
                        Status = a.Status
                    })
                    .ToListAsync();

                // 3. Recent Withdrawal Requests
                var withdrawals = await _context.WalletTransactions
                    .Include(t => t.Doctor)
                    .Where(t => t.TransactionType == "WITHDRAWAL")
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(5)
                    .Select(t => new {
                        t.Id,
                        DoctorName = t.Doctor != null ? t.Doctor.Name : "Unknown",
                        Amount = t.Amount,
                        Status = t.Status,
                        Date = t.TransactionDate
                    })
                    .ToListAsync();

                var dashboardData = new
                {
                    Stats = stats,
                    TriageQueue = triageQueue,
                    Withdrawals = withdrawals
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load admin dashboard." });
            }
        }

        [HttpPost("withdrawals/{id}/approve")]
        public async Task<IActionResult> ApproveWithdrawal(int id)
        {
            try
            {
                var walletTx = await _context.WalletTransactions
                    .Include(t => t.Doctor)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (walletTx == null) return NotFound();
                if (walletTx.Status != "Pending" && walletTx.Status != "pending") 
                    return BadRequest("Request already processed.");

                walletTx.Status = "Approved";
                walletTx.ProcessedDate = DateTime.Now;
                walletTx.ProcessedBy = User.Identity?.Name ?? "AdminAPI";

                if (walletTx.Doctor != null)
                {
                    walletTx.Doctor.TotalWithdrawn += walletTx.Amount;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Withdrawal approved." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to approve withdrawal." });
            }
        }
    }
}

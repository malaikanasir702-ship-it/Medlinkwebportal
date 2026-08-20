using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.ReportedBy)
                .Include(r => r.ReportedDoctor)
                    .ThenInclude(d => d.User)
                .OrderByDescending(r => r.DateReported)
                .ToListAsync();

            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> SuspendDoctor(int doctorId, string reason)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null) return NotFound(new { message = "Doctor not found." });

            doctor.IsSuspended = true;
            doctor.SuspensionReason = reason;

            // Mark reports against this doctor as "Reviewed"
            var openReports = await _context.Reports
                .Where(r => r.ReportedDoctorId == doctorId && r.Status == "Open")
                .ToListAsync();
            foreach (var report in openReports)
            {
                report.Status = "Reviewed";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Doctor suspended successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreDoctor(int doctorId)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null) return NotFound(new { message = "Doctor not found." });

            doctor.IsSuspended = false;
            doctor.SuspensionReason = null;
            doctor.IsAppealing = false;
            doctor.AppealMessage = null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Doctor access restored successfully." });
        }
    }
}

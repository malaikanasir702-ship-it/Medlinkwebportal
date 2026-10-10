using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/reports")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId);
                if (doctor == null) return NotFound(new { message = "Doctor not found" });

                var report = new Report
                {
                    ReportedById = userId,
                    ReportedDoctorId = request.DoctorId,
                    ConsultationId = request.ConsultationId,
                    IssueType = request.IssueType ?? "General",
                    Description = request.Description,
                    Status = "Open",
                    DateReported = System.DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Report submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to submit report." });
            }
        }

        [HttpGet("analytics/summary")]
        public async Task<IActionResult> GetAnalyticsSummary([FromServices] Services.IAnalyticsAggregatorService analyticsService)
        {
            var summary = await analyticsService.GetAdminAnalyticsAsync();
            return Ok(summary);
        }

        [HttpGet("analytics/physician/{doctorUserId}")]
        public async Task<IActionResult> GetPhysicianAnalytics(string doctorUserId, [FromServices] Services.IAnalyticsAggregatorService analyticsService)
        {
            var summary = await analyticsService.GetDoctorAnalyticsAsync(doctorUserId);
            return Ok(summary);
        }

        [HttpGet("analytics/pdf")]
        public async Task<IActionResult> DownloadAnalyticsPdf([FromServices] Services.IAnalyticsAggregatorService analyticsService)
        {
            var pdfBytes = await analyticsService.GenerateEnterpriseReportPdfAsync();
            return File(pdfBytes, "application/pdf", $"MedLink_Enterprise_Report_{System.DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }

    public class SubmitReportRequest
    {
        public int DoctorId { get; set; }
        public int? ConsultationId { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
    }
}

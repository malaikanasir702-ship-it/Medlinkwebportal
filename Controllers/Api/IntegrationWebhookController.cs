using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using MedLinkPortal.Services;

namespace MedLinkPortal.Controllers.Api
{
    [ApiController]
    [Route("api/integration/webhook")]
    public class IntegrationWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IDocumentIngestionService _ingestionService;

        public IntegrationWebhookController(ApplicationDbContext db, IDocumentIngestionService ingestionService)
        {
            _db = db;
            _ingestionService = ingestionService;
        }

        [HttpPost("lab-result")]
        public async Task<IActionResult> ReceiveLabResult([FromBody] JsonElement payload, [FromHeader(Name = "X-Partner-Key")] string? partnerKey)
        {
            var partner = await ValidatePartnerKeyAsync(partnerKey);
            if (partner == null) return Unauthorized(new { error = "Invalid or inactive integration partner key." });

            string fileUrl = "https://medlink.health/webhook/lab/" + Guid.NewGuid();
            string fileName = "Partner_Lab_Result_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string? patientId = null;

            if (payload.TryGetProperty("patientId", out var pidElem))
                patientId = pidElem.GetString();

            if (payload.TryGetProperty("fileName", out var fnElem))
                fileName = fnElem.GetString() ?? fileName;

            if (payload.TryGetProperty("fileUrl", out var fuElem))
                fileUrl = fuElem.GetString() ?? fileUrl;

            var doc = await _ingestionService.QueueDocumentAsync(fileUrl, "LabReport", patientId, null, fileName);
            doc.ExtractedJson = payload.GetRawText();
            await _db.SaveChangesAsync();

            partner.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Lab result received and queued for clinical intelligence processing.",
                documentId = doc.Id,
                status = doc.Status
            });
        }

        [HttpPost("discharge-summary")]
        public async Task<IActionResult> ReceiveDischargeSummary([FromBody] JsonElement payload, [FromHeader(Name = "X-Partner-Key")] string? partnerKey)
        {
            var partner = await ValidatePartnerKeyAsync(partnerKey);
            if (partner == null) return Unauthorized(new { error = "Invalid or inactive integration partner key." });

            string fileUrl = "https://medlink.health/webhook/discharge/" + Guid.NewGuid();
            string fileName = "Partner_Discharge_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string? patientId = null;

            if (payload.TryGetProperty("patientId", out var pidElem))
                patientId = pidElem.GetString();

            var doc = await _ingestionService.QueueDocumentAsync(fileUrl, "DischargeSummary", patientId, null, fileName);
            doc.ExtractedJson = payload.GetRawText();
            await _db.SaveChangesAsync();

            partner.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Discharge summary received and queued.",
                documentId = doc.Id
            });
        }

        [HttpPost("appointment-sync")]
        public async Task<IActionResult> SyncAppointment([FromBody] JsonElement payload, [FromHeader(Name = "X-Partner-Key")] string? partnerKey)
        {
            var partner = await ValidatePartnerKeyAsync(partnerKey);
            if (partner == null) return Unauthorized(new { error = "Invalid or inactive integration partner key." });

            partner.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Appointment sync acknowledged." });
        }

        private async Task<IntegrationPartner?> ValidatePartnerKeyAsync(string? partnerKey)
        {
            if (string.IsNullOrWhiteSpace(partnerKey))
            {
                // Development fallback default partner
                return new IntegrationPartner { Id = 1, PartnerName = "Default Hospital", IsActive = true, ApiKey = "dev" };
            }

            var partner = await _db.IntegrationPartners
                .FirstOrDefaultAsync(p => p.ApiKey == partnerKey && p.IsActive);

            return partner;
        }
    }
}

using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UploadController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IDocumentIngestionService _documentIngestionService;

        public UploadController(ICloudinaryService cloudinaryService, IDocumentIngestionService documentIngestionService)
        {
            _cloudinaryService = cloudinaryService;
            _documentIngestionService = documentIngestionService;
        }

        [HttpPost("Image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var fileUrl = await _cloudinaryService.UploadImageAsync(file, "chat_uploads");
                if (string.IsNullOrEmpty(fileUrl))
                    return StatusCode(500, "Failed to upload image.");

                // If relative path returned by local fallback, prepend host
                if (fileUrl.StartsWith("/"))
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                    fileUrl = $"{baseUrl}{fileUrl}";
                }

                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("clinical-document")]
        public async Task<IActionResult> UploadClinicalDocument(
            [FromForm] IFormFile? file,
            [FromForm] string? fileUrl,
            [FromForm] string? docType,
            [FromForm] string? patientId,
            [FromForm] string? doctorId)
        {
            try
            {
                string finalFileUrl = fileUrl ?? string.Empty;
                string fileName = "ClinicalDocument";

                if (file != null && file.Length > 0)
                {
                    fileName = file.FileName;
                    finalFileUrl = await _cloudinaryService.UploadImageAsync(file, "clinical_documents");
                    if (string.IsNullOrEmpty(finalFileUrl))
                        return StatusCode(500, "Failed to store document file.");

                    if (finalFileUrl.StartsWith("/"))
                    {
                        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                        finalFileUrl = $"{baseUrl}{finalFileUrl}";
                    }
                }
                else if (string.IsNullOrWhiteSpace(finalFileUrl))
                {
                    return BadRequest("Either a file or fileUrl must be provided.");
                }

                // If caller is authenticated, default patient/doctor ID from claims if not supplied
                var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var currentRole = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(patientId) && currentRole != "Doctor" && currentRole != "Admin")
                {
                    patientId = currentUserId;
                }
                else if (string.IsNullOrEmpty(doctorId) && currentRole == "Doctor")
                {
                    doctorId = currentUserId;
                }

                var doc = await _documentIngestionService.QueueDocumentAsync(
                    finalFileUrl,
                    docType ?? "Prescription",
                    patientId,
                    doctorId,
                    fileName
                );

                return Ok(new
                {
                    success = true,
                    message = "Clinical document uploaded and scheduled for automated AI extraction.",
                    documentId = doc.Id,
                    status = doc.Status,
                    fileUrl = doc.FileUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("clinical-documents")]
        public async Task<IActionResult> GetClinicalDocuments([FromQuery] string? patientId, [FromQuery] string? doctorId)
        {
            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var targetPatientId = patientId ?? currentUserId;

            if (!string.IsNullOrEmpty(doctorId))
            {
                var docs = await _documentIngestionService.GetDoctorDocumentsAsync(doctorId);
                return Ok(docs);
            }
            else if (!string.IsNullOrEmpty(targetPatientId))
            {
                var docs = await _documentIngestionService.GetPatientDocumentsAsync(targetPatientId);
                return Ok(docs);
            }

            var allDocs = await _documentIngestionService.GetAllDocumentsAsync();
            return Ok(allDocs);
        }

        [HttpPost("clinical-document/{id:int}/retry")]
        public async Task<IActionResult> RetryDocumentExtraction(int id)
        {
            var success = await _documentIngestionService.RetryExtractionAsync(id);
            if (!success) return NotFound("Document not found.");

            return Ok(new { success = true, message = "Document extraction re-queued." });
        }
    }
}

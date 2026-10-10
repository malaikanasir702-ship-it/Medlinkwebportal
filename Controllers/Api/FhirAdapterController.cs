using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Services;

namespace MedLinkPortal.Controllers.Api
{
    [ApiController]
    [Route("api/fhir")]
    public class FhirAdapterController : ControllerBase
    {
        private readonly IFhirAdapterService _fhirService;

        public FhirAdapterController(IFhirAdapterService fhirService)
        {
            _fhirService = fhirService;
        }

        [HttpGet("Patient/{id}")]
        public async Task<IActionResult> GetPatient(string id)
        {
            var res = await _fhirService.ExportPatientAsFhirAsync(id);
            return Ok(res);
        }

        [HttpGet("DiagnosticReport/{id:int}")]
        public async Task<IActionResult> GetDiagnosticReport(int id)
        {
            var res = await _fhirService.ExportDiagnosticReportAsFhirAsync(id);
            return Ok(res);
        }

        [HttpPost("Patient")]
        public async Task<IActionResult> IngestPatient([FromBody] JsonElement fhirJson)
        {
            try
            {
                var user = await _fhirService.IngestFhirPatientAsync(fhirJson);
                return Created($"/api/fhir/Patient/{user.Id}", new { message = "Patient ingested via FHIR R4", patientId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("DiagnosticReport")]
        public async Task<IActionResult> IngestDiagnosticReport([FromBody] JsonElement fhirJson, [FromHeader(Name = "X-Partner-Key")] string? partnerKey)
        {
            try
            {
                var doc = await _fhirService.IngestFhirDiagnosticReportAsync(fhirJson, partnerKey ?? "DefaultPartner");
                return Created($"/api/fhir/DiagnosticReport/{doc.Id}", new { message = "Diagnostic report ingested into clinical queue", documentId = doc.Id, status = doc.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface IFhirAdapterService
    {
        Task<object> ExportPatientAsFhirAsync(string patientUserId);
        Task<object> ExportDiagnosticReportAsFhirAsync(int reportId);
        Task<ClinicalDocument> IngestFhirDiagnosticReportAsync(JsonElement fhirJson, string partnerApiKey);
        Task<ApplicationUser> IngestFhirPatientAsync(JsonElement fhirJson);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface IDataGovernanceService
    {
        Task LogAccessAsync(string? userId, string role, string? patientUserId, string resource, string? recordId, string accessType, string? ipAddress, string? purpose);
        Task<bool> CheckConsentAsync(string patientUserId, string consentType);
        Task<ConsentRecord> RecordConsentAsync(string patientUserId, string consentType, bool isGranted, string? ipAddress);
        Task<List<ConsentRecord>> GetPatientConsentsAsync(string patientUserId);
        Task<List<DataAccessLog>> GetAccessLogsAsync(int limit = 100);
        Task<List<DataAccessLog>> GetPatientAccessAuditTrailAsync(string patientUserId);
    }
}

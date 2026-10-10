using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface ICredentialingService
    {
        Task<PhysicianCredentialProfile> GetOrCreateProfileAsync(string doctorUserId);
        Task<PhysicianCredentialProfile> RecalculateProfileAsync(string doctorUserId);
        Task<CredentialingScore> GetScoreBreakdownAsync(string doctorUserId);
        Task<List<PhysicianCredentialProfile>> GetAllProfilesAsync();
        Task<bool> UpdateVerificationStatusAsync(string doctorUserId, bool pmcVerified, string? pmcNumber, bool mdcVerified);
    }
}

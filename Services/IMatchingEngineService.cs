using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface IMatchingEngineService
    {
        Task<List<DoctorMatchResult>> FindBestDoctorsAsync(string? patientUserId, string symptoms, string urgency, string? location = null);
        Task<List<EmployerMatchResult>> MatchDoctorToEmployersAsync(string doctorUserId);
        Task<MatchingRequest> RecordMatchingRequestAsync(string? patientUserId, string symptoms, string urgency, List<DoctorMatchResult> results);
        Task<List<MatchingRequest>> GetRecentMatchesAsync(int limit = 50);
        Task<List<EmployerRequirement>> GetEmployerRequirementsAsync();
        Task<EmployerRequirement> AddEmployerRequirementAsync(EmployerRequirement requirement);
        Task<bool> DeleteEmployerRequirementAsync(int id);
    }
}

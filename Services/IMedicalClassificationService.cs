using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface IMedicalClassificationService
    {
        Task<ClassifiedMedicalRecord?> ClassifyAndPersistAsync(int clinicalDocumentId);
        Task<List<ClassifiedMedicalRecord>> GetClassifiedCasesForDoctorAsync(string doctorUserId);
        Task<List<ClassifiedMedicalRecord>> GetAllClassifiedRecordsAsync(int limit = 100);
        Task<List<MedicalClassificationTag>> GetTagsForDoctorAsync(string doctorUserId);
        Task<bool> UpdateClassificationAsync(int recordId, string specialty, string[] icdCodes, string[] procedures);
    }
}

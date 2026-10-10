using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface IDocumentIngestionService
    {
        Task<ClinicalDocument> QueueDocumentAsync(string fileUrl, string docType, string? patientId, string? doctorId, string fileName);
        Task<ClinicalDocument?> GetDocumentAsync(int documentId);
        Task<List<ClinicalDocument>> GetPatientDocumentsAsync(string patientId);
        Task<List<ClinicalDocument>> GetDoctorDocumentsAsync(string doctorId);
        Task<List<ClinicalDocument>> GetAllDocumentsAsync(int limit = 50);
        Task<bool> RetryExtractionAsync(int documentId);
    }
}

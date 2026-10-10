using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface IAiChatService
    {
        Task<AiChatResponse> ProcessMessageAsync(string userId, string message);
        Task<List<AiChatMessage>> GetChatHistoryAsync(string userId);
        Task ClearHistoryAsync(string userId);

        // Enterprise AI Layer
        Task<string> ExtractClinicalDataAsync(string fileUrl, string docType);
        Task<string> ClassifyMedicalDataAsync(string extractedJson);
        Task<string> ExtractRelationshipsAsync(string anonymizedCaseJson);
        Task<string> GenerateMatchReasoningAsync(string doctorName, string doctorSpecialty, string patientSymptoms, string urgency);
    }

    public class AiChatResponse
    {
        public string Reply { get; set; }
        public bool IsComplete { get; set; }
        public List<Doctor> SuggestedDoctors { get; set; } = new List<Doctor>();
    }
}

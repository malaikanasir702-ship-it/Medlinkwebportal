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
    }

    public class AiChatResponse
    {
        public string Reply { get; set; }
        public bool IsComplete { get; set; }
        public List<Doctor> SuggestedDoctors { get; set; } = new List<Doctor>();
    }
}

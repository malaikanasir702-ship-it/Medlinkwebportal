using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class DeIdentificationResult
    {
        public string DeIdentifiedContent { get; set; } = string.Empty;
        public string EncryptedOriginal { get; set; } = string.Empty;
        public int PiiItemsRemoved { get; set; }
        public bool ContainsSensitiveData { get; set; }
    }

    public interface IDeIdentificationService
    {
        DeIdentificationResult ProcessClinicalText(string rawText);
        Task<DeIdentificationResult> ProcessDocumentAsync(int documentId);
    }
}

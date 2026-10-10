using System.Threading.Tasks;
using MedLinkPortal.Models;
using MedLinkPortal.Models.KnowledgeGraph;

namespace MedLinkPortal.Services
{
    public interface IAnonymizationService
    {
        AnonymizedCaseData AnonymizeClassifiedRecord(ClassifiedMedicalRecord record, ApplicationUser? patient);
        string StripPii(string rawClinicalText);
        string HashIdentifier(string identifier);
    }
}

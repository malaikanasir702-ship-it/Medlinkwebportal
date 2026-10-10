using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public interface INeuralReportService
    {
        byte[] GenerateReport(AIHealthReport report, string patientName);
    }
}

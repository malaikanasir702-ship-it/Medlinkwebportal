using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedLinkPortal.Services
{
    public class AdminAnalyticsSummary
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TotalConsultations { get; set; }
        public int TotalClassifiedCases { get; set; }
        public int TotalMatchingRequests { get; set; }
        public Dictionary<string, int> SpecialtyDistribution { get; set; } = new();
        public Dictionary<string, int> TierDistribution { get; set; } = new();
        public List<MonthlyTrendPoint> CaseVolumeTrends { get; set; } = new();
        public List<string> TopDiagnoses { get; set; } = new();
        public double MatchSuccessRate { get; set; } = 92.4;
    }

    public class MonthlyTrendPoint
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DoctorAnalyticsSummary
    {
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public double CredentialScore { get; set; }
        public string TierLevel { get; set; } = "Junior";
        public int MyCaseVolume { get; set; }
        public int SpecialtyAverageVolume { get; set; }
        public int CpdPoints { get; set; }
        public int ReferralsCount { get; set; }
        public List<string> TopProcedures { get; set; } = new();
        public List<double> ScoreGrowthHistory { get; set; } = new();
    }

    public interface IAnalyticsAggregatorService
    {
        Task<AdminAnalyticsSummary> GetAdminAnalyticsAsync();
        Task<DoctorAnalyticsSummary> GetDoctorAnalyticsAsync(string doctorUserId);
        Task<byte[]> GenerateEnterpriseReportPdfAsync();
    }
}

using System.Collections.Generic;

namespace MedLinkPortal.Models
{
    public class DoctorMatchResult
    {
        public int DoctorId { get; set; }
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public double MatchScore { get; set; } // 0 - 100
        public string MatchReason { get; set; } = string.Empty;
        public string CredentialTier { get; set; } = "Junior";
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public decimal ConsultationFee { get; set; }
        public bool IsAvailable { get; set; }
        public List<string> MatchingHighlights { get; set; } = new List<string>();
    }

    public class EmployerMatchResult
    {
        public int EmployerId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string RequiredSpecialty { get; set; } = string.Empty;
        public double MatchScore { get; set; } // 0 - 100
        public string SalaryRange { get; set; } = string.Empty;
        public bool VisaSponsorship { get; set; }
        public List<string> Gaps { get; set; } = new List<string>();
        public List<string> Matches { get; set; } = new List<string>();
        public string Recommendation { get; set; } = string.Empty;
    }
}

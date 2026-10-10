using System;
using System.Collections.Generic;

namespace MedLinkPortal.Models
{
    public class CredentialingScore
    {
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public double OverallScore { get; set; } // 0 - 100
        public string TierLevel { get; set; } = "Junior";

        // Score Breakdown
        public double CaseExperienceScore { get; set; } // max 25
        public double ProcedureProficiencyScore { get; set; } // max 25
        public double ConsultationActivityScore { get; set; } // max 20
        public double CpdEducationScore { get; set; } // max 15
        public double VerificationBonusScore { get; set; } // max 15

        public List<string> Badges { get; set; } = new List<string>();
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> CareerRecommendations { get; set; } = new List<string>();
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}

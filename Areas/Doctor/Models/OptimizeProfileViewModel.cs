using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Areas.Doctor.Models
{
    public class OptimizeProfileViewModel
    {
        [Display(Name = "Advanced Diagnostic Capabilities")]
        public string DiagnosticCapabilities { get; set; } // Comma separated for simplicity, or we can use List<string> but string is easier for tags input

        [Display(Name = "Cross-Institutional Referral Preferences")]
        public string ReferralPreferences { get; set; }

        [Display(Name = "Research Interests & Clinical Trials")]
        public string ResearchInterests { get; set; }

        [Display(Name = "Elite Status Requested")]
        public bool EliteStatusRequested { get; set; }

        public bool IsOptimized { get; set; }
    }
}

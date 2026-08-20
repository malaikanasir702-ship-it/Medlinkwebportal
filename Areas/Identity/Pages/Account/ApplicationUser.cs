using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Areas.Identity.Pages.Account
{
    // Custom ApplicationUser class that extends IdentityUser
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? Name { get; set; }

        [PersonalData]
        public string? FirstName { get; set; }

        [PersonalData]
        public string? LastName { get; set; }

        [PersonalData]
        public string FullName => $"{FirstName} {LastName}";

        [PersonalData]
        public DateTime? DateOfBirth { get; set; }

        [PersonalData]
        public string? ProfileImage { get; set; }

        [PersonalData]
        public bool EmailNotificationsEnabled { get; set; } = true;

        [PersonalData]
        public bool PushNotificationsEnabled { get; set; } = true;

        [PersonalData]
        public bool MarketingEmailsEnabled { get; set; }

        [PersonalData]
        public bool DarkModeEnabled { get; set; }

        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }

        // --- UNIFIED PROFILE EXTENSIONS (From Doctor/Patient Dashboards) ---

        [PersonalData]
        public string? Specialist { get; set; }
        [PersonalData]
        public string? Experience { get; set; }
        [PersonalData]
        public decimal ConsultationFee { get; set; }
        [PersonalData]
        public decimal WalletBalance { get; set; }
        [PersonalData]
        public decimal TotalWithdrawn { get; set; }
        [PersonalData]
        public bool IsAvailable { get; set; } = true;
        [PersonalData]
        public string? Workplace { get; set; }
        [PersonalData]
        public string? ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        // Personal Information
        [PersonalData]
        [Encrypted]
        public string? FatherHusbandName { get; set; }
        [PersonalData]
        public string? Gender { get; set; }
        [PersonalData]
        [Encrypted]
        public string? CNIC { get; set; }
        [PersonalData]
        public string? CNICFrontUrl { get; set; }
        [PersonalData]
        public string? CNICBackUrl { get; set; }

        // Contact Information
        [PersonalData]
        [Encrypted]
        public string? ResidentialAddress { get; set; }
        [PersonalData]
        public string? City { get; set; }
        [PersonalData]
        public string? Province { get; set; }

        // Professional Information
        [PersonalData]
        [Encrypted]
        public string? PMDCRegistrationNumber { get; set; }
        [PersonalData]
        public string? PMDCCertificateUrl { get; set; }
        [PersonalData]
        public DateTime? PMDCValidityDate { get; set; }
        [PersonalData]
        [Encrypted]
        public string? Qualification { get; set; }
        [PersonalData]
        public string? DegreeCertificateUrl { get; set; }

        // Financial & Legal
        [PersonalData]
        [Encrypted]
        public string? BankAccountNumber { get; set; }
        [PersonalData]
        public bool TermsConsent { get; set; }

        // Admin Verification
        [PersonalData]
        [Encrypted]
        public string? AdminRemarks { get; set; }
        [PersonalData]
        public DateTime? ApprovalDate { get; set; }

        [PersonalData]
        public string? VerificationDetails { get; set; }

        [PersonalData]
        public string? ProfilePictureUrl { get; set; }

        [PersonalData]
        public int? LaboratoryId { get; set; }

        [PersonalData]
        public bool IsPro { get; set; } = false;

        [PersonalData]
        public string? CardBrand { get; set; } // e.g., "VISA", "MASTERCARD"

        [PersonalData]
        public string? CardLast4 { get; set; } // e.g., "4242"

        [PersonalData]
        public string? CardExpiry { get; set; } // e.g., "08/27"

        [PersonalData]
        public string? FcmToken { get; set; }

        // --- SignalR Connection Tracking ---
        public string? ActiveSignalRConnectionId { get; set; }
    }
}
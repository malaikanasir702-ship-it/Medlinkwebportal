using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class PhysicianCredentialProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DoctorUserId { get; set; } = string.Empty;

        [ForeignKey("DoctorUserId")]
        public virtual ApplicationUser? DoctorUser { get; set; }

        // Auto-calculated from ClassifiedMedicalRecords
        public int TotalCasesProcessed { get; set; } = 0;
        public int TotalProceduresPerformed { get; set; } = 0;
        public double TotalClinicalHours { get; set; } = 0.0;
        public int TotalConsultations { get; set; } = 0; // From Appointments table
        public int TotalPrescriptions { get; set; } = 0; // From Prescriptions table
        public int TotalReferralsMade { get; set; } = 0; // From DoctorReferrals table
        public int CpdPoints { get; set; } = 0; // From CpdActivities table

        // AI-generated & synthesised
        [MaxLength(100)]
        public string PrimarySpecialty { get; set; } = "General Practice";

        // JSON array of strings
        public string SecondarySpecialties { get; set; } = "[]";

        // JSON array of strings (top 10)
        public string TopProcedures { get; set; } = "[]";

        // JSON array of expertise keywords
        public string ExpertiseTags { get; set; } = "[]";

        public double CredentialScore { get; set; } = 0.0; // 0 - 100

        [MaxLength(50)]
        public string TierLevel { get; set; } = "Junior"; // Junior, Mid, Senior, Expert, Elite

        // Verification Status
        public bool PmcVerified { get; set; } = false;

        [MaxLength(100)]
        public string? PmcNumber { get; set; }

        public bool MdcVerified { get; set; } = false;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime ProfileGeneratedAt { get; set; } = DateTime.UtcNow;
    }
}

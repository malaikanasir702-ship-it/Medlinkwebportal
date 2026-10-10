using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class EmployerRequirement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string HospitalName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = "United Kingdom";

        [Required]
        [MaxLength(100)]
        public string RequiredSpecialty { get; set; } = "Cardiology";

        public int MinExperienceYears { get; set; } = 5;

        public int MinCasesCount { get; set; } = 500;

        // JSON array of strings
        public string RequiredProceduresJson { get; set; } = "[]";

        // JSON array of strings
        public string RequiredCertificationsJson { get; set; } = "[]";

        [MaxLength(100)]
        public string? SalaryRange { get; set; } = "£90,000 - £120,000 / yr";

        public bool VisaSponsorship { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

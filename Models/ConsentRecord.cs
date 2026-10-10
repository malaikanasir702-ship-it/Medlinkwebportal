using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class ConsentRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PatientUserId { get; set; } = string.Empty;

        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser? PatientUser { get; set; }

        // Research, AI_Training, CrossBorderSharing, GeneralCare
        [Required]
        [MaxLength(60)]
        public string ConsentType { get; set; } = "Research";

        public bool IsGranted { get; set; } = true;

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        [MaxLength(20)]
        public string ConsentVersion { get; set; } = "1.0";

        [MaxLength(60)]
        public string? IpAddress { get; set; }
    }
}

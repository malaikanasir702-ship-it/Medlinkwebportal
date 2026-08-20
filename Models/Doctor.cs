using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Models
{
    [Table("Doctors")]
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Specialty { get; set; }

        [Column(TypeName = "decimal(3,1)")]
        [Range(0, 5)]
        public double Rating { get; set; }

        public int Reviews { get; set; }

        public string Image { get; set; }

        [StringLength(50)]
        public string Availability { get; set; }

        public bool Online { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Experience { get; set; }

        [StringLength(100)]
        public string Languages { get; set; }

        [StringLength(100)]
        public string Qualification { get; set; }

        public string? Expertise { get; set; }
        
        [StringLength(500)]
        public string? HospitalAffiliations { get; set; }

        [StringLength(200)]
        public string? ClinicAddress { get; set; }

        [StringLength(1000)]
        public string? ClinicMapUrl { get; set; }

        [StringLength(200)]
        public string? ClinicName { get; set; }

        [StringLength(50)]
        public string? PmdcRegistrationNumber { get; set; }

        /// <summary>Clinic latitude for "Near Me" geolocation filtering</summary>
        [Column(TypeName = "decimal(10,7)")]
        public double? Latitude { get; set; }

        /// <summary>Clinic longitude for "Near Me" geolocation filtering</summary>
        [Column(TypeName = "decimal(10,7)")]
        public double? Longitude { get; set; }


        public string? UserId { get; set; }

        public int SlotDuration { get; set; } = 20;
        public int BufferTime { get; set; } = 5;

        [NotMapped]
        public int UnreadCount { get; set; }

        public int? CurrentPlanId { get; set; }

        [ForeignKey("CurrentPlanId")]
        public virtual SubscriptionPlan? CurrentPlan { get; set; }

        public bool IsSuspended { get; set; } = false;
        
        [StringLength(500)]
        public string? SuspensionReason { get; set; }
        
        public bool IsAppealing { get; set; } = false;
        
        [StringLength(1000)]
        public string? AppealMessage { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Review> PatientReviews { get; set; }
    }
}
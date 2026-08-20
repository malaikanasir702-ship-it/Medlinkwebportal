using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("MedicalTourismPackages")]
    public class MedicalTourismPackage
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; } // Reverse navigation if needed, but Request has PK to this.
        public string? Country { get; set; }

        // Proposed Details
        public int? HospitalId { get; set; }
        [ForeignKey("HospitalId")]
        public virtual Hospital Hospital { get; set; }

        public int? DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }

        public string? TreatmentDuration { get; set; } // e.g. "5 days"
        public string? RecoveryDays { get; set; } // e.g. "2 weeks"

        // Tourism
        public string? TourPlanDetails { get; set; }
        public string? HotelDetails { get; set; }
        public string? AirportPickup { get; set; } = "Included";

        // Cost
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public bool IsAcceptedByUser { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

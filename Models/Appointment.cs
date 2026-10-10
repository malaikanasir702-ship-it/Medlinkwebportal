using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("Appointments")]
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PatientName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [StringLength(20)]
        public string TimeSlot { get; set; }

        [StringLength(20)]
        public string? ConsultationType { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public int? DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        
        [ForeignKey("UserId")]
        public MedLinkPortal.Areas.Identity.Pages.Account.ApplicationUser? Patient { get; set; }
        
        public string UserId { get; set; }
        public string? StripeSessionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
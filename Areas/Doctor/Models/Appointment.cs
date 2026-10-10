using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Areas.Doctor.Models;
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; }
        
        [ForeignKey("DoctorId")]
        public ApplicationUser Doctor { get; set; }

        [Required]
        public string PatientId { get; set; }
        
        [ForeignKey("PatientId")]
        public ApplicationUser Patient { get; set; }

        [Required]
        public DateTime ScheduledTime { get; set; }

        public int DurationMinutes { get; set; } = 30;

        public string? ConsultationType { get; set; } // e.g., "Normal Checkup", "Follow-up"

        public string Status { get; set; } = "Confirmed"; // "Confirmed", "Pending", "Completed", "Cancelled"
    }


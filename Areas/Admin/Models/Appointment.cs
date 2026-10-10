using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Areas.Admin.Models;
    public class Appointment
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Patient is required")]
        [StringLength(50, ErrorMessage = "Patient ID cannot exceed 50 characters")]
        public string PatientId { get; set; }
        
        [Required(ErrorMessage = "Physician is required")]
        [StringLength(50, ErrorMessage = "Physician ID cannot exceed 50 characters")]
        public string PhysicianId { get; set; }
        
        [Required(ErrorMessage = "Appointment time is required")]
        public DateTime AppointmentTime { get; set; }
        
        [Required(ErrorMessage = "Consultation type is required")]
        [RegularExpression("^(Video|Audio|Chat)$", ErrorMessage = "Consultation type must be Video, Audio, or Chat")]
        public string ConsultationType { get; set; } // Video, Audio, Chat
        
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(Scheduled|Completed|Cancelled|In-Progress)$", ErrorMessage = "Status must be Scheduled, Completed, Cancelled, or In-Progress")]
        public string Status { get; set; } // Scheduled, Completed, Cancelled, In-Progress
        // Navigation properties (not virtual to keep it simple with EF Core 10 defaults for this project)
        public Patient? Patient { get; set; }
        public Physician? Physician { get; set; }
    }


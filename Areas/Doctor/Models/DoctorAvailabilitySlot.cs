using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Areas.Doctor.Models;

    public class DoctorAvailabilitySlot
    {
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [ForeignKey("DoctorId")]
        public ApplicationUser? Doctor { get; set; }

        [Required]
        public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday...

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;
    }


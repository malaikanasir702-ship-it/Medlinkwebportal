using System;
using System.Collections.Generic;

namespace MedLinkPortal.Areas.Doctor.Models
{
    public class DoctorDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int ActiveTreatments { get; set; }
        public int FollowUpNeeded { get; set; }
        public double AverageRating { get; set; }
        public List<PatientCardViewModel> RecentPatients { get; set; } = new();
        public List<MedLinkPortal.Models.Notification> Notifications { get; set; } = new();
        public List<Appointment> TodayAppointments { get; set; } = new();
    }

    public class PatientCardViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // e.g., "Follow-up", "Stable", "Urgent"
        public string StatusColor { get; set; } = string.Empty; // CSS classes for badges
        public DateTime? LastVisit { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string RecordStatus { get; set; } = string.Empty;
        public DateTime? NextAppointment { get; set; }
    }
}

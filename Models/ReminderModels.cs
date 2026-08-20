namespace MedLinkPortal.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReminderType { get; set; } // Exercise, Medication, Appointment, Custom
        public DateTime ScheduledTime { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; } // Daily, Weekly, Monthly
        public bool IsActive { get; set; } = true;
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }

    public class ReminderViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReminderType { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public string TimeUntil { get; set; }
        public string FormattedTime { get; set; }
    }

    public class CreateReminderModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReminderType { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. Prescription Templates
    // ─────────────────────────────────────────────────────────────────────────
    [Table("PrescriptionTemplates")]
    public class PrescriptionTemplate
    {
        [Key] public int Id { get; set; }

        [Required] public string DoctorId { get; set; } = string.Empty;

        [Required][StringLength(100)] public string Name { get; set; } = string.Empty; // e.g. "Fever Bundle"

        [Encrypted][Required] public string Diagnosis { get; set; } = string.Empty;

        [Encrypted][Required] public string MedicationsJson { get; set; } = "[]";

        [Encrypted][StringLength(2000)] public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DoctorId")] public virtual ApplicationUser? Doctor { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Patient Follow-up Reminders
    // ─────────────────────────────────────────────────────────────────────────
    [Table("PatientFollowUpReminders")]
    public class PatientFollowUpReminder
    {
        [Key] public int Id { get; set; }

        [Required] public string DoctorId { get; set; } = string.Empty;
        [Required] public string PatientId { get; set; } = string.Empty;

        public int? AppointmentId { get; set; }

        [Encrypted][Required][StringLength(500)] public string Message { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }      // When to send the reminder
        public bool IsSent { get; set; } = false;
        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DoctorId")] public virtual ApplicationUser? Doctor { get; set; }
        [ForeignKey("PatientId")] public virtual ApplicationUser? Patient { get; set; }
        [ForeignKey("AppointmentId")] public virtual Appointment? Appointment { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Virtual Waiting Room
    // ─────────────────────────────────────────────────────────────────────────
    [Table("WaitingRoomEntries")]
    public class WaitingRoomEntry
    {
        [Key] public int Id { get; set; }

        [Required] public int DoctorDbId { get; set; }   // Doctor.Id (not UserId)
        [Required] public string PatientId { get; set; } = string.Empty;
        [Required] public int AppointmentId { get; set; }

        public int QueuePosition { get; set; }           // 1 = next up
        public int EstimatedWaitMinutes { get; set; }

        [StringLength(20)] public string Status { get; set; } = "Waiting"; // Waiting | Called | Done | Left

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CalledAt { get; set; }

        [ForeignKey("DoctorDbId")] public virtual Doctor? Doctor { get; set; }
        [ForeignKey("PatientId")] public virtual ApplicationUser? Patient { get; set; }
        [ForeignKey("AppointmentId")] public virtual Appointment? Appointment { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Doctor-to-Doctor Referrals
    // ─────────────────────────────────────────────────────────────────────────
    [Table("DoctorReferrals")]
    public class DoctorReferral
    {
        [Key] public int Id { get; set; }

        [Required] public string ReferringDoctorUserId { get; set; } = string.Empty;
        [Required] public string ReceivingDoctorUserId { get; set; } = string.Empty;
        [Required] public string PatientId { get; set; } = string.Empty;

        public int? AppointmentId { get; set; }

        [Encrypted][Required][StringLength(500)] public string Reason { get; set; } = string.Empty;
        [Encrypted][StringLength(2000)] public string? ClinicalNotes { get; set; }

        [StringLength(20)] public string Status { get; set; } = "Pending"; // Pending | Accepted | Completed | Declined

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        [ForeignKey("ReferringDoctorUserId")] public virtual ApplicationUser? ReferringDoctor { get; set; }
        [ForeignKey("ReceivingDoctorUserId")] public virtual ApplicationUser? ReceivingDoctor { get; set; }
        [ForeignKey("PatientId")] public virtual ApplicationUser? Patient { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. CPD / CME Credit Tracker
    // ─────────────────────────────────────────────────────────────────────────
    [Table("CpdActivities")]
    public class CpdActivity
    {
        [Key] public int Id { get; set; }

        [Required] public string DoctorId { get; set; } = string.Empty;

        [Required][StringLength(200)] public string Title { get; set; } = string.Empty;
        [StringLength(100)] public string? Provider { get; set; }           // e.g. "PMDC", "AKU"
        [StringLength(50)] public string ActivityType { get; set; } = "Webinar"; // Webinar | Conference | Course | Workshop | Publication

        public int CreditPoints { get; set; } = 1;
        public DateTime ActivityDate { get; set; }
        public string? CertificateUrl { get; set; }

        [StringLength(500)] public string? Description { get; set; }
        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DoctorId")] public virtual ApplicationUser? Doctor { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Clinic Staff Members
    // ─────────────────────────────────────────────────────────────────────────
    [Table("ClinicStaff")]
    public class ClinicStaff
    {
        [Key] public int Id { get; set; }

        [Required] public string DoctorId { get; set; } = string.Empty;  // owning doctor
        [Required] public string StaffUserId { get; set; } = string.Empty; // linked app user

        [Required][StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required][StringLength(200)] public string Email { get; set; } = string.Empty;
        [StringLength(20)] public string Phone { get; set; } = string.Empty;
        [StringLength(50)] public string Role { get; set; } = "Receptionist"; // Receptionist | Nurse | Assistant

        public bool IsActive { get; set; } = true;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DoctorId")] public virtual ApplicationUser? Doctor { get; set; }
        [ForeignKey("StaffUserId")] public virtual ApplicationUser? StaffUser { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Patient Review Replies
    // ─────────────────────────────────────────────────────────────────────────
    [Table("ReviewReplies")]
    public class ReviewReply
    {
        [Key] public int Id { get; set; }

        [Required] public int ReviewId { get; set; }
        [Required] public string DoctorId { get; set; } = string.Empty;

        [Required][StringLength(1000)] public string ReplyText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ReviewId")] public virtual Review? Review { get; set; }
        [ForeignKey("DoctorId")] public virtual ApplicationUser? Doctor { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. Voice Notes (Doctor personal clinical notes via voice)
    // ─────────────────────────────────────────────────────────────────────────
    [Table("DoctorVoiceNotes")]
    public class DoctorVoiceNote
    {
        [Key] public int Id { get; set; }

        [Required] public string DoctorId { get; set; } = string.Empty;
        public string? PatientId { get; set; }
        public int? AppointmentId { get; set; }

        [Encrypted][Required] public string TranscribedText { get; set; } = string.Empty;
        [StringLength(200)] public string? AudioUrl { get; set; } // Optional audio file URL

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DoctorId")] public virtual ApplicationUser? Doctor { get; set; }
        [ForeignKey("PatientId")] public virtual ApplicationUser? Patient { get; set; }
        [ForeignKey("AppointmentId")] public virtual Appointment? Appointment { get; set; }
    }
}

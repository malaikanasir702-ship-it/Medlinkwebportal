using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        public string ReportedById { get; set; }

        [ForeignKey("ReportedById")]
        public virtual ApplicationUser ReportedBy { get; set; }

        public int ReportedDoctorId { get; set; }

        [ForeignKey("ReportedDoctorId")]
        public virtual Doctor ReportedDoctor { get; set; }

        public int? ConsultationId { get; set; }

        [ForeignKey("ConsultationId")]
        public virtual Appointment Consultation { get; set; }

        [Required]
        [StringLength(100)]
        public string IssueType { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime DateReported { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// e.g. Open, Reviewed, Resolved, Dismissed
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Open";

        public string? AdminNotes { get; set; }
    }
}

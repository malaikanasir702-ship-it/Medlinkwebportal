using System;
using System.ComponentModel.DataAnnotations;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required]
        public string Action { get; set; }

        public string Details { get; set; }

        public string Status { get; set; } = "Success"; // Success, Failed, Warning

        public string IPAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}

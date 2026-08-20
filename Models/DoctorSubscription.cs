using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Models
{
    [Table("DoctorSubscriptions")]
    public class DoctorSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DoctorUserId { get; set; } = string.Empty;

        [ForeignKey("DoctorUserId")]
        public virtual ApplicationUser? DoctorUser { get; set; }

        [Required]
        public int PlanId { get; set; }

        [ForeignKey("PlanId")]
        public virtual SubscriptionPlan? Plan { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        public DateTime? ExpiryDate { get; set; } // null for permanent or starter

        public string? StripeSessionId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

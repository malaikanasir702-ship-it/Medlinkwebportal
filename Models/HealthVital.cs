using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    public class HealthVital
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public string VitalType { get; set; } // HeartRate, Temperature, BloodOxygen, Steps

        public string Value { get; set; }

        public string Unit { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual Areas.Identity.Pages.Account.ApplicationUser User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("SubscriptionPlans")]
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int PatientLimit { get; set; } // e.g. 50 for Starter, -1 for Unlimited

        [StringLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Features as JSON or pipe-separated string for simplicity in this implementation
        public string? Features { get; set; } 
        
        public DateTime CreatedAt { get; set; } = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}

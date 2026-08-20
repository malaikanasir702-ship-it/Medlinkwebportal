using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("RiderRatings")]
    public class RiderRating
    {
        [Key]
        public int Id { get; set; }

        public int RiderId { get; set; }
        [ForeignKey("RiderId")]
        public virtual Rider? Rider { get; set; }

        public int OrderId { get; set; }

        [Required]
        [MaxLength(20)]
        public string OrderType { get; set; } = string.Empty;  // "PharmacyOrder" | "LabBooking"

        public double Rating { get; set; }         // 1.0–5.0

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("Riders")]
    public class Rider
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // FK → AspNetUsers.Id

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string VehicleType { get; set; } = "Motorcycle";  // "Motorcycle", "Bicycle", "Van"

        [MaxLength(20)]
        public string? VehicleNumber { get; set; }  // e.g. "ABC-1234"

        public double AverageRating { get; set; } = 5.0;  // 0.0–5.0

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

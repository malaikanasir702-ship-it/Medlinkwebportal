using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("Medicines")]
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Brand { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public int? StockQuantity { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool? PrescriptionRequired { get; set; }

        public bool? IsActive { get; set; } = true;

        public string? ImageUrl { get; set; } = "/images/medicines/default.png"; // Default image

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

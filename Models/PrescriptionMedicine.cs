using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("PrescriptionMedicines")]
    public class PrescriptionMedicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }
        [ForeignKey("PrescriptionId")]
        public virtual Prescription Prescription { get; set; }

        [Required]
        public int MedicineId { get; set; }
        [ForeignKey("MedicineId")]
        public virtual Medicine Medicine { get; set; }

        [Required]
        public string Dosage { get; set; } // e.g., 500mg

        [Required]
        public string Frequency { get; set; } // e.g., 1-0-1

        [Required]
        public string Duration { get; set; } // e.g., 5 Days

        public int Quantity { get; set; } // Auto-calculated quantity to buy

        public string? Instructions { get; set; }
    }
}

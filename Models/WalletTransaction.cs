using MedLinkPortal.Areas.Identity.Pages.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("WalletTransactions")]
    public class WalletTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } // ApplicationUser Id
        public ApplicationUser Doctor { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; } // "EARNING", "WITHDRAWAL"

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Completed"; // "Completed", "Pending", "Failed"

        // Optional: Link to appointment if it's an earning
        public int? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        // Platform fee tracking (for withdrawals)
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; } = 0;

        // Admin processing tracking
        [StringLength(450)]
        public string? ProcessedBy { get; set; }

        public DateTime? ProcessedDate { get; set; }
    }
}

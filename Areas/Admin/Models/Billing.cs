using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Areas.Admin.Models
{
    public class Billing
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Patient is required")]
        [StringLength(50, ErrorMessage = "Patient ID cannot exceed 50 characters")]
        public string PatientId { get; set; }
        
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999999.99")]
        public decimal Amount { get; set; }
        
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(PENDING|PAID|PARTIALLY-PAID)$", ErrorMessage = "Status must be PENDING, PAID, or PARTIALLY-PAID")]
        public string Status { get; set; } // Pending, Paid, Partially-Paid
        
        [Required(ErrorMessage = "Date generated is required")]
        public DateTime DateGenerated { get; set; }
        // Insurance Details (SRS 2.7)
        
        [StringLength(200, ErrorMessage = "Insurance provider cannot exceed 200 characters")]
        public string? InsuranceProvider { get; set; }
        
        [StringLength(100, ErrorMessage = "Insurance policy number cannot exceed 100 characters")]
        public string? InsurancePolicyNumber { get; set; }
        public Patient? Patient { get; set; }
    }
}

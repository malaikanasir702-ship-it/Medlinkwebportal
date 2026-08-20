using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Areas.Admin.Models;
    public class Physician
    {
        [Required(ErrorMessage = "Physician ID is required")]
        [StringLength(50, ErrorMessage = "Physician ID cannot exceed 50 characters")]
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Specialty is required")]
        [StringLength(100, ErrorMessage = "Specialty cannot exceed 100 characters")]
        public string Specialty { get; set; } = string.Empty;
        
        [Range(0, 100, ErrorMessage = "Experience must be between 0 and 100 years")]
        public int Experience { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone is required")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string Phone { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Office location is required")]
        [StringLength(200, ErrorMessage = "Office location cannot exceed 200 characters")]
        public string Office { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "PMDC Number cannot exceed 50 characters")]
        public string? PmdcRegistrationNumber { get; set; }
        
        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
        public string Bio { get; set; } = string.Empty;
        
        public string? ProfileImage { get; set; } // Base64

        [NotMapped]
        public string? ApprovalStatus { get; set; } // Pending, Approved, Rejected

        [NotMapped]
        public string? UserId { get; set; }

        [NotMapped]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }
    }


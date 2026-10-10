using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Add this for IFormFile

namespace MedLinkPortal.Models
{
    public class PharmacyProfileViewModel
    {
        public string? Username { get; set; }
        public string? Email { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Pharmacy Name")]
        public string? Workplace { get; set; } // Mapping to Workplace in ApplicationUser

        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "Address")]
        public string? ResidentialAddress { get; set; }

        [Display(Name = "Profile Image")]
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Upload New Image")]
        public IFormFile? ProfileImage { get; set; }

        public bool IsEmailVerified { get; set; }
    }
}

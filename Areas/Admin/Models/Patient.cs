using System.ComponentModel.DataAnnotations;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Areas.Admin.Models;
    public class Patient
    {
        [Required(ErrorMessage = "Patient ID is required")]
        [StringLength(50, ErrorMessage = "Patient ID cannot exceed 50 characters")]
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Patient name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Diagnostic is required")]
        [Encrypted]
        [StringLength(500, ErrorMessage = "Diagnostic cannot exceed 500 characters")]
        public string Diagnostic { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(STABLE|OBSERVATION|CRITICAL)$", ErrorMessage = "Status must be STABLE, OBSERVATION, or CRITICAL")]
        public string Status { get; set; } = "STABLE"; // STABLE, OBSERVATION, CRITICAL
        
        [Required(ErrorMessage = "Node is required")]
        [StringLength(100, ErrorMessage = "Node cannot exceed 100 characters")]
        public string Node { get; set; } = string.Empty;
        // Extended Details for High-Fidelity Views
        public DateTime DateRegistered { get; set; } = DateTime.Now;
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-30);
        public string Gender { get; set; } = "Not Specified";
        
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string Phone { get; set; } = string.Empty;
        
        [Encrypted]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;
        public string StatusColor => Status switch
        {
            "STABLE" => "bg-emerald-500",
            "OBSERVATION" => "bg-amber-500",
            "CRITICAL" => "bg-red-500",
            _ => "bg-slate-500"
        };
    }


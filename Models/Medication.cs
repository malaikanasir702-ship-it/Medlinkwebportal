using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class Medication
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string Dosage { get; set; }
        
        public string Schedule { get; set; }
        
        public bool Taken { get; set; }
        
        public string UserId { get; set; } // Link to user
    }
}

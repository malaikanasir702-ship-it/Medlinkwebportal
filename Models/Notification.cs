using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public string Icon { get; set; } // Lucide icon name
        
        public string Color { get; set; } // blue, emerald, amber, etc.
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public bool IsRead { get; set; } = false;
        
        public string UserId { get; set; } // Link to ApplicationUser
    }
}

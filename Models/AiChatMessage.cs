using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class AiChatMessage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string Role { get; set; } // "user" or "assistant"
        
        [Required]
        public string Content { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

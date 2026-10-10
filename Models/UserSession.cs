using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class UserSession
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        public string? UserAgent { get; set; }
        public string? IPAddress { get; set; }
        public string? DeviceName { get; set; }
        public string? Location { get; set; }
        
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        
        public bool IsRevoked { get; set; } = false;
        
        // This will be matched against a cookie value or similar to identify the specific session
        public string? SessionIdentifier { get; set; }
    }
}

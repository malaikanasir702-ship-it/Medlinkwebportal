using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models
{
    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Endpoint { get; set; }

        public string P256dh { get; set; }

        public string Auth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

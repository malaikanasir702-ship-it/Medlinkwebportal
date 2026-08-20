using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Encrypted]
        public string? Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; } // "Everyone" or "Me" (for simpler logic we'll use flag)

        public string? MessageType { get; set; } = "Text"; // "Text", "Call", "System"

        // Attachment Support
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; } // "Image", "Video", "Document"
        public string? AttachmentName { get; set; }

        // Optional: Link to Doctor if one side is always a doctor entry
        public int? DoctorId { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }
}

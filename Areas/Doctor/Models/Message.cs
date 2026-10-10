using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Areas.Doctor.Models;
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }
        
        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; }
        
        [ForeignKey("ReceiverId")]
        public ApplicationUser Receiver { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? AttachmentPath { get; set; }
        
        public string? AttachmentType { get; set; } // "image", "file", etc.
    }


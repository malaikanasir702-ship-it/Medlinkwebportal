using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("FamilyLinks")]
    public class FamilyLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string RequesterId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string MemberId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Relationship { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending";  // pending | accepted | rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RequesterId")]
        public virtual ApplicationUser? Requester { get; set; }

        [ForeignKey("MemberId")]
        public virtual ApplicationUser? MemberUser { get; set; }
    }
}

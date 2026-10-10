using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("Departments")]
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string Icon { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public int Specialists { get; set; }

        [StringLength(50)]
        public string Color { get; set; }

        // Store services as JSON or comma-separated
        [StringLength(1000)]
        public List<string> Services { get; set; }
    }
}
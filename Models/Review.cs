using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public string PatientId { get; set; } // Links to AspNetUsers Id

        [Required]
        [StringLength(100)]
        public string PatientName { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property is optional here depending on need, 
        // but strict FK constraint usually requires Doctor entity locally or foreign key config.
        // For simplicity and to avoid circular dependency issues in JSON serialization, 
        // we might just keep the ID, or add [JsonIgnore] if we add the navigation prop.
        [ForeignKey("DoctorId")]
        [System.Text.Json.Serialization.JsonIgnore] 
        public virtual Doctor Doctor { get; set; }
    }
}

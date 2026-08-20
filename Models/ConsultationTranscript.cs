using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    public class ConsultationTranscript
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public string SpeakerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SpeakerName { get; set; }

        [Required]
        [MaxLength(20)]
        public string SpeakerRole { get; set; } // "Doctor" or "Patient"

        [Encrypted]
        [Required]
        public string OriginalText { get; set; }

        [Encrypted]
        [Required]
        public string EnglishTranslation { get; set; }

        [Encrypted]
        [Required]
        public string UrduTranslation { get; set; }

        [MaxLength(50)]
        public string DetectedLanguage { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        // Navigation property
        [ForeignKey("AppointmentId")]
        public virtual Appointment Appointment { get; set; }
    }
}

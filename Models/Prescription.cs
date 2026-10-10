using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    [Table("Prescriptions")]
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }
        public virtual Appointment Appointment { get; set; }

        [Required]
        public string DoctorId { get; set; }

        [Required]
        public string PatientId { get; set; }

        [Encrypted]
        [Required]
        [StringLength(1000)]
        public string Diagnosis { get; set; }

        [Encrypted]
        [Required]
        public string MedicationsJson { get; set; } // JSON string of [{name, dosage, frequency}]

        [Encrypted]
        [StringLength(2000)]
        public string Notes { get; set; }

        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual System.Collections.Generic.ICollection<PrescriptionMedicine> PrescriptionMedicines { get; set; } = new System.Collections.Generic.List<PrescriptionMedicine>();
    }
}

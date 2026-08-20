using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models
{
    [Table("Hospitals")]
    public class Hospital
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "Pakistan";

        public string Specialties { get; set; } // Comma separated

        public bool IsVerified { get; set; } = true;

        public string ImageUrl { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }
        
        // Navigation properties if we want to link existing doctors to hospitals later
        // or effectively we can use HospitalAffiliations string in Doctor model for loose coupling
        // but for tight coupling:
        // public virtual ICollection<Doctor> Doctors { get; set; }
    }
}

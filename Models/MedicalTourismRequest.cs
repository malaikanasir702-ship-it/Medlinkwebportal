using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    public enum TourismRequestType
    {
        Inbound, // Come to Pakistan
        Outbound // Go Abroad
    }

    public enum RequestStatus
    {
        Pending,
        UnderReview,
        PackagePrepared, // For Inbound
        CoordinatorAssigned, // For Outbound
        PaymentPending,
        TravelScheduled,
        TreatmentCompleted,
        TourCompleted,
        Cancelled
    }

    [Table("MedicalTourismRequests")]
    public class MedicalTourismRequest
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; } // Link to ApplicationUser

        public TourismRequestType RequestType { get; set; }

        // Common Fields
        public string? SourceCountry { get; set; } // Where they are coming from or going from
        public string? PreferredCountry { get; set; } // Destination
        public string? PreferredCity { get; set; } // Destination City

        [Encrypted]
        [Required]
        public string TreatmentType { get; set; } = string.Empty;

        public string? BudgetRange { get; set; }

        // Inbound Specific
        public bool TravelWithFamily { get; set; }
        public string? InterestedTourLocations { get; set; } // JSON or Comma Separated

        // Patient Add-On Preferences (Inbound)
        /// <summary>Does patient want hotel accommodation included?</summary>
        public bool WantsHotel { get; set; } = false;

        /// <summary>Preferred hotel tier: Budget | Standard | Premium | Not Required</summary>
        public string? HotelPreference { get; set; } = "Not Required";

        /// <summary>Does patient want airport pickup/transport included?</summary>
        public bool WantsAirportPickup { get; set; } = false;

        /// <summary>Preferred transport tier: Standard | VIP | Not Required</summary>
        public string? TransportPreference { get; set; } = "Not Required";

        /// <summary>Number of nights hotel stay needed</summary>
        public int? HotelNights { get; set; }

        // Outbound Specific
        public bool PassportAvailable { get; set; }

        public string? MedicalReportsUrl { get; set; }

        [Encrypted]
        public string? AdditionalNotes { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // For Inbound: Link to a Package
        public int? AssignedPackageId { get; set; }
        [ForeignKey("AssignedPackageId")]
        public virtual MedicalTourismPackage? AssignedPackage { get; set; }

        // Pre-Arrival Online Consultation (auto-created when package is assigned)
        public int? PreArrivalAppointmentId { get; set; }
        [ForeignKey("PreArrivalAppointmentId")]
        public virtual Appointment? PreArrivalAppointment { get; set; }
    }
}

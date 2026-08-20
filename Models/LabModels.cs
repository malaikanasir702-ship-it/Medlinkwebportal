using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    public class City
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public virtual ICollection<Laboratory> Laboratories { get; set; }
    }

    public class Laboratory
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? LogoUrl { get; set; }
        public double Rating { get; set; }
        public bool HomeCollectionAvailable { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }

        public int CityId { get; set; }
        [ForeignKey("CityId")]
        public virtual City City { get; set; }

        public virtual ICollection<MedicalTest> MedicalTests { get; set; }
    }

    public class MedicalTestCategory
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public virtual ICollection<MedicalTest> MedicalTests { get; set; }
    }

    public class MedicalTest
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public string ReportTime { get; set; }
        public string SampleType { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual MedicalTestCategory Category { get; set; }

        public int LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory Laboratory { get; set; }
    }

    public class LabBooking
    {
        public int Id { get; set; }
        public string PatientId { get; set; }
        public int LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory Laboratory { get; set; }

        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        [Required]
        [Encrypted]
        public string PatientName { get; set; }
        [Required]
        [Encrypted]
        public string PhoneNumber { get; set; }
        [Encrypted]
        public string Address { get; set; }
        public DateTime PreferredDate { get; set; }
        public bool IsHomeCollection { get; set; }

        public int? RiderId { get; set; }

        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }

        public LabBookingStatus Status { get; set; } = LabBookingStatus.Booked;
        public DateTime BookingDate { get; set; } = DateTime.Now;

        public virtual ICollection<LabBookingItem> BookingItems { get; set; }
        public virtual ICollection<LabTestResult> TestResults { get; set; }
    }

    public enum LabBookingStatus
    {
        Booked = 0,
        CollectorOnWay = 1,
        SampleCollected = 2,
        Processing = 3,
        Ready = 4,
        RiderAssigned = 5    // NEW — rider assigned for home collection
    }

    public class LabBookingItem
    {
        public int Id { get; set; }
        public int LabBookingId { get; set; }
        [ForeignKey("LabBookingId")]
        public virtual LabBooking LabBooking { get; set; }

        public int MedicalTestId { get; set; }
        [ForeignKey("MedicalTestId")]
        public virtual MedicalTest MedicalTest { get; set; }
    }

    public class LabTestResult
    {
        public int Id { get; set; }
        public int LabBookingId { get; set; }
        [ForeignKey("LabBookingId")]
        public virtual LabBooking LabBooking { get; set; }

        [Encrypted]
        public string ReportUrl { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}

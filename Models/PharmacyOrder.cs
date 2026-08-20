using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedLinkPortal.Attributes;

namespace MedLinkPortal.Models
{
    public enum PharmacyOrderStatus
    {
        Pending = 0,
        Accepted = 1,
        Packed = 2,
        RiderAssigned = 3,   // NEW — rider assigned, tracking begins
        PickedUp = 4,        // NEW — rider has picked up from pharmacy
        OnTheWay = 5,        // NEW — rider en-route to patient
        Delivered = 6,       // was Dispatched/Delivered (shifted)
        Cancelled = 7        // was 4/5
    }

    public enum PaymentMethod
    {
        CashOnDelivery,
        OnlinePayment
    }

    [Table("PharmacyOrders")]
    public class PharmacyOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } // Link to ApplicationUser

        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public int? PrescriptionId { get; set; }
        [ForeignKey("PrescriptionId")]
        public virtual Prescription Prescription { get; set; }

        public PharmacyOrderStatus Status { get; set; } = PharmacyOrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Encrypted]
        public string ShippingAddress { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

        public string? PharmacistId { get; set; } // Assigned pharmacist

        public int? RiderId { get; set; }

        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<PharmacyOrderItem> OrderItems { get; set; } = new List<PharmacyOrderItem>();
    }

    [Table("PharmacyOrderItems")]
    public class PharmacyOrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual PharmacyOrder Order { get; set; }

        public int MedicineId { get; set; }
        [ForeignKey("MedicineId")]
        public virtual Medicine Medicine { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}

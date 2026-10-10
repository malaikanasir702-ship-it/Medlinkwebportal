using System;
using System.Collections.Generic;

namespace MedLinkPortal.Models.Api
{
    public class PharmacyOrderRequest
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public int PaymentMethod { get; set; } // 0: CashOnDelivery, 1: OnlinePayment
        public int? PrescriptionId { get; set; }
        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }
        public List<PharmacyOrderItemRequest> Items { get; set; } = new List<PharmacyOrderItemRequest>();
    }

    public class PharmacyOrderItemRequest
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }

    public class PharmacyOrderResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }
        public List<PharmacyOrderItemResponse> Items { get; set; } = new List<PharmacyOrderItemResponse>();
    }

    public class PharmacyOrderItemResponse
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string MedicineBrand { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

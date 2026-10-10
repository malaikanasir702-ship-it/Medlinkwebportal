using System.Collections.Generic;
namespace MedLinkPortal.Areas.Admin.Models
{
    public class InvoiceDetailsViewModel
    {
        public Billing Invoice { get; set; }
        public Patient Patient { get; set; }
        public List<MedicalRecord> MedicalRecords { get; set; }
        public List<Appointment> RecentAppointments { get; set; }
    }
}

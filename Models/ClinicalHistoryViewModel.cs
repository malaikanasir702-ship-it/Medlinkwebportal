using System.Collections.Generic;

namespace MedLinkPortal.Models
{
    public class ClinicalHistoryViewModel
    {
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public List<HealthRecord> HealthRecords { get; set; }
        public List<Prescription> Prescriptions { get; set; }
        public List<MedicalTourismRequest> PreviousRequests { get; set; }
    }
}

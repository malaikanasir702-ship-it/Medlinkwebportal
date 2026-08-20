using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Areas.Doctor.Models;

namespace MedLinkPortal.Areas.Doctor.Models
{
    public class PatientRecordListViewModel
    {
        public ApplicationUser Patient { get; set; }
        public DateTime? NextAppointmentTime { get; set; }
        public int? AppointmentId { get; set; }
        public bool IsAppointmentStarted => NextAppointmentTime.HasValue && (DateTime.Now >= NextAppointmentTime.Value || NextAppointmentTime.Value.Date == DateTime.Today);
    }
}

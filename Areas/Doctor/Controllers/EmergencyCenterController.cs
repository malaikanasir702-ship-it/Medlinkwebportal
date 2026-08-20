using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLinkPortal.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class EmergencyCenterController : Controller
    {
        public IActionResult Index()
        {
            // Pass the DoctorId to the view so JavaScript can filter alerts meant for this doctor
            // (or let them see all alerts if you want a global emergency room)
            var claimsPrincipal = User;
            // Assuming DoctorId is stored in a claim or we can just fetch it. 
            // We'll let JS handle it globally for now to ensure all doctors see it if no specific doctor is found.
            return View();
        }
    }
}

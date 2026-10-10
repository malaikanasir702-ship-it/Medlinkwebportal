using Microsoft.AspNetCore.Mvc;

namespace MedLinkPortal.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class AdminController : Controller
    {
        public IActionResult AdminDashBoard()
        {
            return View();
        }
    }
}

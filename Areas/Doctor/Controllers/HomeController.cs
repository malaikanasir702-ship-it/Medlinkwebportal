using System.Diagnostics;
using MedLinkPortal.Areas.Doctor.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedLinkPortal.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class HomeController : Controller
    {
        // Actions removed as per requirement regarding landing/login pages
        public IActionResult Index()
        {
            return RedirectToAction("DoctorDashBoard", "Doctor");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

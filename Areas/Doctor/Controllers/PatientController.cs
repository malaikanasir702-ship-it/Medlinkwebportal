using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Areas.Doctor.Models;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedLinkPortal.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> PatientDashBoard()
        {
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            ViewBag.Doctors = doctors;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(MedLinkPortal.Areas.Doctor.Models.Appointment appointment)
        {
            appointment.PatientId = _userManager.GetUserId(User) ?? "";
            
            // Basic validation for testing
            if (!string.IsNullOrEmpty(appointment.DoctorId) && appointment.ScheduledTime != default)
            {
                _context.DoctorAppointments.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment booked successfully!";
            }
            else
            {
                TempData["Error"] = "Please select a doctor and time.";
            }

            return RedirectToAction(nameof(PatientDashBoard));
        }
    }
}

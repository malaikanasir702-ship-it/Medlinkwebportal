using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;
using Appointment = MedLinkPortal.Areas.Admin.Models.Appointment;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.AdminAppointments
                .Include(a => a.Patient)
                .Include(a => a.Physician)
                .ToListAsync();
            // For the dropdowns in the create/edit modals
            ViewBag.Patients = await _context.AdminPatients.ToListAsync();
            ViewBag.Physicians = await _context.AdminPhysicians.ToListAsync();
            return View(appointments);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid data" });
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid data" });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.AdminAppointments.FindAsync(id);
            if (appointment != null)
            {
                _context.AdminAppointments.Remove(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}

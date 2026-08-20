using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;
using System;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PrescriptionController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var records = await _context.AdminMedicalRecords
                .Include(r => r.Patient)
                .Include(r => r.Physician)
                .OrderByDescending(r => r.DateCreated)
                .ToListAsync();
            ViewBag.Patients = await _context.AdminPatients.ToListAsync();
            ViewBag.Physicians = await _context.AdminPhysicians.ToListAsync();
            return View(records);
        }
        [HttpPost]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var record = await _context.AdminMedicalRecords.FindAsync(id);
            if (record != null)
            {
                record.IsApproved = !record.IsApproved;
                await _context.SaveChangesAsync();
                return Json(new { success = true, isApproved = record.IsApproved });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public async Task<IActionResult> Create(MedicalRecord record)
        {
            if (ModelState.IsValid)
            {
                record.DateCreated = DateTime.Now;
                _context.Add(record);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public async Task<IActionResult> Edit(MedicalRecord updatedRecord)
        {
            if (ModelState.IsValid)
            {
                var record = await _context.AdminMedicalRecords.FindAsync(updatedRecord.Id);
                if (record == null) return NotFound();
                record.Title = updatedRecord.Title;
                record.Content = updatedRecord.Content;
                record.RecordType = updatedRecord.RecordType;
                record.PatientId = updatedRecord.PatientId;
                record.PhysicianId = updatedRecord.PhysicianId;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}

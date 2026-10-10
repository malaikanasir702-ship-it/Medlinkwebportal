using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MedicalTourismManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicalTourismManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/MedicalTourismManager
        public async Task<IActionResult> Index()
        {
            var requests = await _context.MedicalTourismRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        // GET: Admin/MedicalTourismManager/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.MedicalTourismRequests
                .Include(r => r.AssignedPackage)
                .ThenInclude(p => p.Hospital)
                .Include(r => r.AssignedPackage)
                .ThenInclude(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            ViewBag.Hospitals = new SelectList(_context.Hospitals, "Id", "Name");
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "Name");

            return View(request);
        }

        // POST: Create Package for Inbound
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePackage(int requestId, MedicalTourismPackage package)
        {
            var request = await _context.MedicalTourismRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            package.RequestId = requestId;
            _context.Add(package);
            await _context.SaveChangesAsync();

            // Link to Request
            request.AssignedPackageId = package.Id;
            request.Status = RequestStatus.PackagePrepared; // Update Status
            _context.Update(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = requestId });
        }
        
        // POST: Update Status (e.g. for Assignments)
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, RequestStatus status)
        {
            var request = await _context.MedicalTourismRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
        // GET: Admin/MedicalTourismManager/ClinicalHistory?userId=xyz
        public async Task<IActionResult> ClinicalHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var user = await _context.Users.FindAsync(userId);
            var patientName = user?.FirstName + " " + user?.LastName;
            if (string.IsNullOrWhiteSpace(patientName)) patientName = user?.Email?.Split('@')[0] ?? userId;

            var records = await _context.HealthRecords
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Appointment)
                .ThenInclude(a => a.Doctor)
                .Include(p => p.PrescriptionMedicines)
                .ThenInclude(pm => pm.Medicine)
                .Where(p => p.PatientId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var requests = await _context.MedicalTourismRequests
                .Include(r => r.AssignedPackage)
                .ThenInclude(p => p.Hospital)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new ClinicalHistoryViewModel
            {
                PatientId = userId,
                PatientName = patientName,
                HealthRecords = records,
                Prescriptions = prescriptions,
                PreviousRequests = requests
            };

            return View(model);
        }

        // GET: Admin/MedicalTourismManager/PrescriptionDetails/5
        public async Task<IActionResult> PrescriptionDetails(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Appointment)
                .ThenInclude(a => a.Doctor)
                .Include(p => p.PrescriptionMedicines)
                .ThenInclude(pm => pm.Medicine)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null) return NotFound();

            var patient = await _context.Users.FindAsync(prescription.PatientId);
            ViewBag.PatientName = patient?.FirstName + " " + patient?.LastName;

            return View(prescription);
        }

        // GET: Admin/MedicalTourismManager/ExportDossier?userId=...
        public async Task<IActionResult> ExportDossier(string userId)
        {
            var patient = await _context.Users.FindAsync(userId);
            if (patient == null) return NotFound();

            var records = await _context.HealthRecords.Where(r => r.UserId == userId).ToListAsync();
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Appointment).ThenInclude(a => a.Doctor)
                .Include(p => p.PrescriptionMedicines).ThenInclude(pm => pm.Medicine)
                .Where(p => p.PatientId == userId)
                .ToListAsync();
            var requests = await _context.MedicalTourismRequests.Where(r => r.UserId == userId).ToListAsync();

            var model = new ClinicalHistoryViewModel
            {
                PatientId = userId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                HealthRecords = records,
                Prescriptions = prescriptions,
                PreviousRequests = requests
            };

            return View(model);
        }
    }
}

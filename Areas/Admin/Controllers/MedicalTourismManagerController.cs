using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Services;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MedicalTourismManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public MedicalTourismManagerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
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
            var request = await _context.MedicalTourismRequests
                .Include(r => r.AssignedPackage)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound();

            package.RequestId = requestId;
            _context.Add(package);
            await _context.SaveChangesAsync();

            // Link to Request
            request.AssignedPackageId = package.Id;
            request.Status = RequestStatus.PackagePrepared;
            _context.Update(request);

            // ── Auto-create Pre-Arrival Online Consultation appointment ──────
            if (!string.IsNullOrEmpty(request.UserId) && package.DoctorId.HasValue)
            {
                var patient = await _userManager.FindByIdAsync(request.UserId);
                var doctor = await _context.Doctors.FindAsync(package.DoctorId.Value);

                if (patient != null && doctor != null)
                {
                    // Schedule consultation 3 days from now (admin can adjust later)
                    var consultDate = DateTime.UtcNow.AddDays(3).Date;

                    var preArrivalAppt = new Appointment
                    {
                        PatientName = patient.FullName ?? patient.Email ?? "Tourism Patient",
                        Email = patient.Email ?? "",
                        PhoneNumber = patient.PhoneNumber,
                        AppointmentDate = consultDate,
                        TimeSlot = "10:00 AM",
                        ConsultationType = "Tourism Pre-Arrival",
                        Notes = $"Pre-arrival online consultation for Medical Tourism Request #{request.Id}. Treatment: {request.TreatmentType}",
                        DoctorId = package.DoctorId.Value,
                        UserId = request.UserId,
                        Status = "Confirmed",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Appointments.Add(preArrivalAppt);
                    await _context.SaveChangesAsync();

                    // Link appointment back to tourism request
                    request.PreArrivalAppointmentId = preArrivalAppt.Id;
                    _context.Update(request);

                    // Notify patient
                    await _notificationService.CreateAndSendNotificationAsync(
                        request.UserId,
                        "Pre-Arrival Consultation Scheduled",
                        $"Your online consultation with Dr. {doctor.Name} has been scheduled for {consultDate:MMM dd, yyyy} at 10:00 AM before your arrival in Pakistan.",
                        "video",
                        "blue"
                    );

                    // Notify doctor
                    if (!string.IsNullOrEmpty(doctor.UserId))
                    {
                        var patientDisplay = patient.FullName ?? patient.Email ?? "International Patient";
                        await _notificationService.CreateAndSendNotificationAsync(
                            doctor.UserId,
                            "New Tourism Pre-Arrival Consultation",
                            $"You have a pre-arrival online consultation with {patientDisplay} scheduled for {consultDate:MMM dd, yyyy} at 10:00 AM. Treatment: {request.TreatmentType}. Patient is coming from {request.SourceCountry}.",
                            "globe",
                            "indigo"
                        );
                    }
                }
            }

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

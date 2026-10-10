using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;
using System.Linq;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<MedLinkPortal.Areas.Identity.Pages.Account.ApplicationUser> _userManager;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> _roleManager;

        public PatientController(ApplicationDbContext context, 
            Microsoft.AspNetCore.Identity.UserManager<MedLinkPortal.Areas.Identity.Pages.Account.ApplicationUser> userManager,
            Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch real users from Identity
            var patients = await _userManager.GetUsersInRoleAsync("Patient");
            
            // Fetch any existing extended data from AdminPatients table
            var adminData = _context.AdminPatients.ToList().ToDictionary(p => p.Id);
            
            var model = patients.Select(u => {
                // If we have admin data, use it, otherwise default
                if (adminData.TryGetValue(u.Id, out var existing))
                {
                    // Ensure Name matches User if changed, or keep Admin override? 
                    // Let's prefer User Name if available, or fallback to Admin Data
                    existing.Name = u.Name ?? u.UserName ?? existing.Name; 
                    existing.Phone = u.PhoneNumber ?? existing.Phone;
                    return existing;
                }
                
                return new Patient
                {
                    Id = u.Id,
                    Name = u.Name ?? u.UserName ?? "Unregistered User",
                    Status = "STABLE",
                    Diagnostic = "Pending Evaluation",
                    Node = "General Intake",
                    DateRegistered = DateTime.Now, // Approximation
                    Phone = u.PhoneNumber ?? "N/A"
                };
            }).OrderByDescending(p => p.DateRegistered).ToList();

            return View(model);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Patient patient)
        {
            if (patient == null) return BadRequest(new { message = "Invalid patient data" });
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Validation failed", errors = errors });
            }
            // Check if ID already exists
            if (_context.AdminPatients.Any(p => p.Id == patient.Id))
            {
                return Conflict(new { message = $"Patient ID {patient.Id} is already registered in the system." });
            }
            try 
            {
                _context.AdminPatients.Add(patient);
                _context.SaveChanges();
                return Ok(patient);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Database error: " + ex.Message });
            }
        }
        [HttpPut]
        public IActionResult Edit([FromBody] Patient patient)
        {
            if (patient == null) return BadRequest(new { message = "Invalid patient data" });
            
            // Check if exists in AdminPatients
            var existing = _context.AdminPatients.FirstOrDefault(p => p.Id == patient.Id);
            
            if (existing == null)
            {
                // If not in AdminPatients (e.g. fresh from Identity), we must create the record
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { message = "Validation failed", errors = errors });
                }
                _context.AdminPatients.Add(patient);
            }
            else
            {
                existing.Name = patient.Name;
                existing.Diagnostic = patient.Diagnostic;
                existing.Status = patient.Status;
                existing.Node = patient.Node;
                // Don't update ID
            }
            
            _context.SaveChanges();
            return Ok(patient);
        }
        [HttpDelete]
        public IActionResult Delete(string id)
        {
            var patient = _context.AdminPatients.FirstOrDefault(p => p.Id == id);
            // We only delete from AdminPatients extended data, not the actual User account for now to be safe
            if (patient == null) return NotFound();
            _context.AdminPatients.Remove(patient);
            _context.SaveChanges();
            return Ok();
        }
    }
}

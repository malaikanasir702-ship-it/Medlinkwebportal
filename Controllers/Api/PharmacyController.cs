using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/pharmacy")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PharmacyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PharmacyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var orders = await _context.PharmacyOrders
                    .Where(o => o.Status != PharmacyOrderStatus.Cancelled)
                    .ToListAsync();

                var stats = new
                {
                    TodayOrders = orders.Count(o => o.CreatedAt.Date == today),
                    PendingOrders = orders.Count(o => o.Status == PharmacyOrderStatus.Pending),
                    LowStockItems = await _context.Medicines.CountAsync(m => (m.StockQuantity ?? 0) < 20),
                    TotalRevenue = orders.Sum(o => o.TotalAmount)
                };

                var recentOrders = await _context.PharmacyOrders
                    .Include(o => o.Patient)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new {
                        o.Id,
                        PatientName = (o.Patient != null ? o.Patient.FirstName + " " + o.Patient.LastName : "Patient"),
                        Amount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        Date = o.CreatedAt
                    })
                    .ToListAsync();

                var topMedicines = await _context.PharmacyOrderItems
                    .Include(oi => oi.Medicine)
                    .GroupBy(oi => oi.MedicineId)
                    .Select(g => new {
                        Name = g.First().Medicine.Name,
                        Sold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    })
                    .OrderByDescending(x => x.Sold)
                    .Take(5)
                    .ToListAsync();

                return Ok(new
                {
                    Stats = stats,
                    RecentOrders = recentOrders,
                    TopMedicines = topMedicines
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load pharmacy dashboard." });
            }
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var medicines = await _context.Medicines
                    .OrderBy(m => m.Name)
                    .Select(m => new {
                        m.Id,
                        m.Name,
                        m.Brand,
                        m.StockQuantity,
                        m.Price,
                        m.PrescriptionRequired
                    })
                    .ToListAsync();
                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load inventory." });
            }
        }

        [HttpPost("orders/{id}/status")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] int status)
        {
            try
            {
                var order = await _context.PharmacyOrders.FindAsync(id);
                if (order == null) return NotFound();

                order.Status = (PharmacyOrderStatus)status;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update order status." });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMedicines(string term)
        {
            try
            {
                var query = _context.Medicines.Where(m => m.IsActive == true);
                
                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(m => m.Name.Contains(term));
                }

                var medicines = await query
                    .Select(m => new { 
                        id = m.Id, 
                        name = m.Name, 
                        brand = m.Brand, 
                        price = m.Price,
                        prescriptionRequired = m.PrescriptionRequired
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Search failed." });
            }
        }

        [HttpPost("prescription/submit")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> SubmitStructuredPrescription([FromBody] PrescriptionRequest model)
        {
            try
            {
                if (model == null || model.AppointmentId == 0) return BadRequest(new { success = false, message = "Invalid data" });

                var appointment = await _context.Appointments.FindAsync(model.AppointmentId);
                if (appointment == null) return NotFound(new { success = false, message = "Appointment not found" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized(new { success = false, message = "Unauthorized: User not found" });

                var prescription = await _context.Prescriptions
                    .Include(p => p.PrescriptionMedicines)
                    .FirstOrDefaultAsync(p => p.AppointmentId == model.AppointmentId);

                if (prescription == null)
                {
                    prescription = new Prescription
                    {
                        AppointmentId = model.AppointmentId,
                        DoctorId = user.Id,
                        PatientId = appointment.UserId,
                        CreatedAt = DateTime.UtcNow,
                        MedicationsJson = "Structured",
                    };
                    _context.Prescriptions.Add(prescription);
                }
                else if (prescription.IsLocked)
                {
                    return BadRequest(new { success = false, message = "Prescription is locked" });
                }

                prescription.Diagnosis = model.Diagnosis;
                prescription.Notes = model.Notes;
                prescription.IsLocked = model.Finalize;

                _context.PrescriptionMedicines.RemoveRange(prescription.PrescriptionMedicines);
                
                foreach (var med in model.Medicines)
                {
                    _context.PrescriptionMedicines.Add(new PrescriptionMedicine
                    {
                        Prescription = prescription,
                        MedicineId = med.MedicineId,
                        Dosage = med.Dosage,
                        Frequency = med.Frequency,
                        Duration = med.Duration,
                        Quantity = med.Quantity,
                        Instructions = med.Instructions
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, prescriptionId = prescription.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to submit prescription." });
            }
        }
    }

    public class PrescriptionRequest
    {
        public int AppointmentId { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
        public bool Finalize { get; set; }
        public List<PrescriptionMedicineRequest> Medicines { get; set; }
    }

    public class PrescriptionMedicineRequest
    {
        public int MedicineId { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Duration { get; set; }
        public int Quantity { get; set; }
        public string Instructions { get; set; }
    }
}

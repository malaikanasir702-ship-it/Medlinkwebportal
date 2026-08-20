using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MedLinkPortal.Areas.Doctor.Models;

// Request DTOs for DoctorController
public record WithdrawalRequest(decimal Amount);
public record ConstraintsRequest(int? SlotDuration, int? BufferTime);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record SlotRequest(int Id, string DayOfWeek, string StartTime, string EndTime, bool IsActive);

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/doctor")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class DoctorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public DoctorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet("suspension-status")]
        public async Task<IActionResult> GetSuspensionStatus()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound(new { message = "Doctor profile not found" });

                return Ok(new {
                    isSuspended = doctor.IsSuspended,
                    reason = doctor.SuspensionReason,
                    isAppealing = doctor.IsAppealing,
                    appealMessage = doctor.AppealMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get suspension status." });
            }
        }

        [HttpPost("appeal-suspension")]
        public async Task<IActionResult> AppealSuspension([FromBody] Models.Api.AppealRequest request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound(new { message = "Doctor profile not found" });

                if (!doctor.IsSuspended) return BadRequest(new { message = "You are not suspended." });

                doctor.IsAppealing = true;
                doctor.AppealMessage = request.Message;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Appeal submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to submit appeal." });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                var doctor = await _context.Doctors
                    .Include(d => d.CurrentPlan)
                    .FirstOrDefaultAsync(d => d.UserId == userId);

                if (doctor == null) return NotFound("Doctor profile not found.");

                var today = DateTime.Today;
                var monthAgo = DateTime.Now.AddDays(-30);

                var stats = new
                {
                    TotalPatients = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id)
                        .Select(a => a.UserId)
                        .Distinct()
                        .CountAsync(),
                    ActiveTreatments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate >= monthAgo)
                        .Select(a => a.UserId)
                        .Distinct()
                        .CountAsync(),
                    FollowUpNeeded = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate >= today)
                        .CountAsync(),
                    Rating = doctor.Rating,
                    WalletBalance = user?.WalletBalance ?? 0
                };

                var todayAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == today)
                    .OrderBy(a => a.AppointmentDate)
                    .Select(a => new {
                        a.Id,
                        PatientName = a.PatientName ?? (a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "Unknown"),
                        Time = a.TimeSlot,
                        Type = a.ConsultationType,
                        Status = a.Status,
                        PatientImage = (a.Patient != null ? (a.Patient.ProfileImage ?? "https://picsum.photos/seed/" + a.Id + "/100/100") : "https://picsum.photos/seed/" + a.Id + "/100/100")
                    })
                    .ToListAsync();

                var recentPatients = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.Id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => new {
                        PatientId = a.UserId,
                        Name = a.PatientName ?? (a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "Patient"),
                        LastVisit = a.AppointmentDate,
                        Image = (a.Patient != null ? (a.Patient.ProfileImage ?? "https://picsum.photos/seed/" + a.UserId + "/100/100") : "https://picsum.photos/seed/" + a.UserId + "/100/100")
                    })
                    .Take(20)
                    .ToListAsync();

                var uniquePatients = recentPatients
                    .GroupBy(p => p.PatientId)
                    .Select(g => g.First())
                    .Take(6)
                    .ToList();

                return Ok(new
                {
                    Stats = stats,
                    TodayAppointments = todayAppointments,
                    RecentPatients = uniquePatients,
                    IsPro = user?.IsPro ?? false,
                    PlanName = doctor.CurrentPlan?.Name ?? "Standard"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load doctor dashboard." });
            }
        }
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule([FromQuery] string period = "Week", [FromQuery] DateTime? date = null)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound("Doctor profile not found.");

                var currentDate = date ?? DateTime.Today;
                DateTime startDate, endDate;

                switch (period?.ToLower())
                {
                    case "day":
                        startDate = currentDate.Date;
                        endDate = startDate.AddDays(1).AddTicks(-1);
                        break;
                    case "month":
                        startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                        endDate = startDate.AddMonths(1).AddTicks(-1);
                        break;
                    case "week":
                    default:
                        int diff = (7 + (currentDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                        startDate = currentDate.AddDays(-1 * diff).Date;
                        endDate = startDate.AddDays(7).AddTicks(-1);
                        break;
                }

                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date >= startDate.Date && a.AppointmentDate.Date <= endDate.Date)
                    .OrderBy(a => a.AppointmentDate)
                    .Select(a => new {
                        a.Id,
                        PatientName = a.PatientName ?? (a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "Unknown"),
                        PatientId = a.UserId,
                        PatientImage = (a.Patient != null ? (a.Patient.ProfileImage ?? "https://picsum.photos/seed/" + a.Id + "/100/100") : "https://picsum.photos/seed/" + a.Id + "/100/100"),
                        ScheduledTime = a.AppointmentDate,
                        TimeSlot = a.TimeSlot,
                        Type = a.ConsultationType,
                        Status = a.Status
                    })
                    .ToListAsync();

                return Ok(new { 
                    startDate = startDate, 
                    endDate = endDate, 
                    period = period ?? "Week",
                    appointments = appointments 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load schedule." });
            }
        }

        [HttpGet("patients")]
        public async Task<IActionResult> GetPatients()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                var patientIds = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Select(a => a.UserId)
                    .Distinct()
                    .ToListAsync();

                var patients = await _userManager.Users
                    .Where(u => patientIds.Contains(u.Id))
                    .Select(u => new {
                        u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = u.Email,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth,
                        Image = u.ProfileImage ?? "https://picsum.photos/seed/" + u.Id + "/100/100"
                    })
                    .ToListAsync();

                var result = new List<object>();
                foreach (var patient in patients)
                {
                    var nextAppt = await _context.Appointments
                        .Where(a => a.UserId == patient.Id && a.DoctorId == doctor.Id && a.AppointmentDate >= DateTime.Today.AddDays(-1) && a.Status != "Completed")
                        .OrderBy(a => a.AppointmentDate)
                        .Select(a => new { a.Id, a.AppointmentDate, a.ConsultationType })
                        .FirstOrDefaultAsync();

                    var lastAppt = await _context.Appointments
                        .Where(a => a.UserId == patient.Id && a.DoctorId == doctor.Id && a.Status == "Completed")
                        .OrderByDescending(a => a.AppointmentDate)
                        .Select(a => new { a.AppointmentDate, a.ConsultationType })
                        .FirstOrDefaultAsync();

                    result.Add(new {
                        Patient = patient,
                        NextAppointment = nextAppt,
                        LastAppointment = lastAppt
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load patients." });
            }
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                var patientIds = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Select(a => a.UserId)
                    .Distinct()
                    .ToListAsync();

                var patients = await _userManager.Users
                    .Where(u => patientIds.Contains(u.Id))
                    .Select(u => new {
                        u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Image = u.ProfileImage ?? "https://picsum.photos/seed/" + u.Id + "/100/100"
                    })
                    .ToListAsync();

                var rng = new Random();
                var activeChats = patients.Select(patient => new {
                    Patient = patient,
                    LastMessage = "I have uploaded my recent test reports.",
                    LastMessageTime = DateTime.Now.AddHours(-rng.Next(1, 48)),
                    UnreadCount = rng.Next(0, 3)
                }).ToList();

                return Ok(activeChats.OrderByDescending(c => c.LastMessageTime));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load messages." });
            }
        }

        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                var rawSlots = await _context.DoctorAvailabilitySlots
                    .Where(s => s.DoctorId == userId)
                    .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                    .ToListAsync();

                var formattedSlots = rawSlots.Select(s => new {
                    s.Id,
                    s.DayOfWeek,
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    s.IsActive
                });

                return Ok(new {
                    IsAvailable = doctor.Online,
                    SlotDuration = doctor.SlotDuration,
                    BufferTime = doctor.BufferTime,
                    Slots = formattedSlots
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load availability." });
            }
        }

        [HttpPost("availability/toggle")]
        public async Task<IActionResult> ToggleAvailability()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                doctor.Online = !doctor.Online;
                await _context.SaveChangesAsync();

                return Ok(new { isAvailable = doctor.Online });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to toggle availability." });
            }
        }

        [HttpPost("availability/constraints")]
        public async Task<IActionResult> UpdateConstraints([FromBody] ConstraintsRequest config)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                if (config.SlotDuration.HasValue) doctor.SlotDuration = config.SlotDuration.Value;
                if (config.BufferTime.HasValue) doctor.BufferTime = config.BufferTime.Value;

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update constraints." });
            }
        }

        [HttpPost("availability/slots")]
        public async Task<IActionResult> SaveSlot([FromBody] SlotRequest model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var slot = new DoctorAvailabilitySlot
                {
                    Id = model.Id,
                    DoctorId = userId,
                    DayOfWeek = model.DayOfWeek,
                    StartTime = TimeSpan.Parse(model.StartTime),
                    EndTime = TimeSpan.Parse(model.EndTime),
                    IsActive = model.IsActive
                };
                
                if (slot.Id == 0) _context.DoctorAvailabilitySlots.Add(slot);
                else _context.Update(slot);

                await _context.SaveChangesAsync();
                return Ok(new { success = true, id = slot.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to save slot." });
            }
        }

        [HttpDelete("availability/slots/{id}")]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var slot = await _context.DoctorAvailabilitySlots.FindAsync(id);
                if (slot == null) return NotFound();
                if (slot.DoctorId != userId) return Forbid();

                _context.DoctorAvailabilitySlots.Remove(slot);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete slot." });
            }
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound();

                var transactions = await _context.WalletTransactions
                    .Where(t => t.DoctorId == userId)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new {
                        t.Id,
                        t.Amount,
                        t.TransactionType,
                        t.Description,
                        Date = t.TransactionDate,
                        t.Status,
                        t.AppointmentId
                    })
                    .ToListAsync();

                var stats = new {
                    TotalEarnings = transactions.Where(t => t.TransactionType == "EARNING").Sum(t => (decimal?)t.Amount) ?? 0,
                    PendingWithdrawals = transactions.Where(t => t.TransactionType == "WITHDRAWAL" && t.Status == "Pending").Sum(t => (decimal?)t.Amount) ?? 0,
                    TotalWithdrawn = user.TotalWithdrawn
                };

                return Ok(new {
                    Balance = user.WalletBalance,
                    Stats = stats,
                    Transactions = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load wallet." });
            }
        }

        [HttpPost("wallet/withdraw")]
        public async Task<IActionResult> RequestWithdrawal([FromBody] WithdrawalRequest request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound();

                decimal amount = request.Amount;
                if (amount < 100) return BadRequest("Minimum withdrawal is PKR 100.");
                if (amount > user.WalletBalance) return BadRequest("Insufficient balance.");

                decimal fee = amount * 0.02m;
                decimal net = amount - fee;

                var transaction = new WalletTransaction
                {
                    DoctorId = userId,
                    Amount = amount,
                    TransactionType = "WITHDRAWAL",
                    Description = "Withdrawal request",
                    Status = "Pending",
                    PlatformFee = fee,
                    NetAmount = net,
                    TransactionDate = DateTime.Now
                };

                user.WalletBalance -= amount;
                _context.WalletTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, balance = user.WalletBalance });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Withdrawal request failed." });
            }
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var logs = await _context.AuditLogs
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(50)
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load audit logs." });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                return Ok(new {
                    Name = user.FirstName + " " + user.LastName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Specialty = doctor.Specialty,
                    Qualification = doctor.Qualification,
                    Experience = doctor.Experience,
                    ConsultationFee = user.ConsultationFee,
                    Bio = doctor.Description,
                    ProfileImage = user.ProfileImage ?? "https://picsum.photos/seed/" + userId + "/200/200",
                    IsPro = user.IsPro,
                    CompletionPercentage = 85
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load profile." });
            }
        }

        [HttpPost("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] decimal? consultationFee, [FromForm] string? bio, [FromForm] string? phone, IFormFile? profileImage)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                if (consultationFee != null) user.ConsultationFee = consultationFee.Value;
                if (bio != null) doctor.Description = bio;
                if (phone != null) user.PhoneNumber = phone;

                if (profileImage != null && profileImage.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                    var filePath = Path.Combine("wwwroot/uploads/profiles", fileName);
                    
                    Directory.CreateDirectory("wwwroot/uploads/profiles");
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }
                    user.ProfileImage = "/uploads/profiles/" + fileName;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, profileImage = user.ProfileImage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update profile." });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound();

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded) return Ok(new { success = true });

                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to change password." });
            }
        }

        [HttpPost("export-records")]
        public async Task<IActionResult> ExportRecords()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.Id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"<h1>Clinical Export Report for Dr. {user.FirstName} {user.LastName}</h1>");
                sb.AppendLine($"<p>Generated on: {DateTime.Now:MMM dd, yyyy HH:mm}</p>");
                sb.AppendLine("<table border='1' style='border-collapse: collapse; width: 100%;'>");
                sb.AppendLine("<tr style='background-color: #f2f2f2;'><th>Date</th><th>Patient</th><th>Type</th><th>Status</th></tr>");

                foreach (var apt in appointments)
                {
                    var patientName = apt.PatientName ?? (apt.Patient != null ? apt.Patient.FirstName + " " + apt.Patient.LastName : "Unknown");
                    sb.AppendLine($"<tr><td>{apt.AppointmentDate:MMM dd, yyyy}</td><td>{patientName}</td><td>{apt.ConsultationType}</td><td>{apt.Status}</td></tr>");
                }

                sb.AppendLine("</table>");
                sb.AppendLine("<p>Total Records: " + appointments.Count + "</p>");

                var emailSender = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
                await emailSender.SendEmailAsync(user.Email, "MedLink Clinical Export", sb.ToString());

                return Ok(new { success = true, message = "Exported. You will receive it by email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Export failed. Please try again." });
            }
        }
    }
}

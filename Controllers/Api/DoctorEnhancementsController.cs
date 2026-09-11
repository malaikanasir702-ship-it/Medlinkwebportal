using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ── Request DTOs ─────────────────────────────────────────────────────────────
public record SaveTemplateRequest(int Id, string Name, string Diagnosis, string MedicationsJson, string? Notes);
public record FollowUpRequest(string PatientId, int? AppointmentId, string Message, DateTime ScheduledAt);
public record ReferralRequest(string ReceivingDoctorUserId, string PatientId, int? AppointmentId, string Reason, string? ClinicalNotes);
public record ReferralRespondRequest(int ReferralId, string Status);
public record CpdActivityRequest(int Id, string Title, string? Provider, string ActivityType, int CreditPoints, DateTime ActivityDate, string? Description);
public record StaffAddRequest(string Name, string Email, string Phone, string Role);
public record ReviewReplyRequest(int ReviewId, string ReplyText);
public record WaitRoomCallRequest(int EntryId);
public record PatientReportRequest(string PatientId);

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/doctor")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class DoctorEnhancementsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;

        public DoctorEnhancementsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IEmailSender emailSender)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
            _emailSender = emailSender;
        }

        // ══════════════════════════════════════════════════════════════════════
        // 1. PRESCRIPTION TEMPLATES
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("prescription-templates")]
        public async Task<IActionResult> GetTemplates()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var templates = await _db.PrescriptionTemplates
                .Where(t => t.DoctorId == userId)
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Diagnosis,
                    t.MedicationsJson,
                    t.Notes,
                    t.CreatedAt,
                    t.UpdatedAt
                })
                .ToListAsync();

            return Ok(templates);
        }

        [HttpPost("prescription-templates")]
        public async Task<IActionResult> SaveTemplate([FromBody] SaveTemplateRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (req.Id == 0)
            {
                var template = new PrescriptionTemplate
                {
                    DoctorId    = userId,
                    Name        = req.Name,
                    Diagnosis   = req.Diagnosis,
                    MedicationsJson = req.MedicationsJson,
                    Notes       = req.Notes,
                    CreatedAt   = DateTime.UtcNow,
                    UpdatedAt   = DateTime.UtcNow
                };
                _db.PrescriptionTemplates.Add(template);
                await _db.SaveChangesAsync();
                return Ok(new { success = true, id = template.Id });
            }
            else
            {
                var template = await _db.PrescriptionTemplates
                    .FirstOrDefaultAsync(t => t.Id == req.Id && t.DoctorId == userId);
                if (template == null) return NotFound();

                template.Name        = req.Name;
                template.Diagnosis   = req.Diagnosis;
                template.MedicationsJson = req.MedicationsJson;
                template.Notes       = req.Notes;
                template.UpdatedAt   = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
        }

        [HttpDelete("prescription-templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var userId = _userManager.GetUserId(User);
            var t = await _db.PrescriptionTemplates
                .FirstOrDefaultAsync(x => x.Id == id && x.DoctorId == userId);
            if (t == null) return NotFound();

            _db.PrescriptionTemplates.Remove(t);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 2. PATIENT FOLLOW-UP REMINDERS
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("follow-up-reminders")]
        public async Task<IActionResult> GetFollowUps()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var reminders = await _db.PatientFollowUpReminders
                .Include(r => r.Patient)
                .Where(r => r.DoctorId == userId)
                .OrderByDescending(r => r.ScheduledAt)
                .Select(r => new
                {
                    r.Id,
                    PatientId   = r.PatientId,
                    PatientName = r.Patient != null
                        ? r.Patient.FirstName + " " + r.Patient.LastName
                        : "Unknown",
                    r.Message,
                    r.ScheduledAt,
                    r.IsSent,
                    r.SentAt,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(reminders);
        }

        [HttpPost("follow-up-reminders")]
        public async Task<IActionResult> CreateFollowUp([FromBody] FollowUpRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var patient = await _userManager.FindByIdAsync(req.PatientId);
            if (patient == null) return NotFound(new { message = "Patient not found" });

            var reminder = new PatientFollowUpReminder
            {
                DoctorId      = userId,
                PatientId     = req.PatientId,
                AppointmentId = req.AppointmentId,
                Message       = req.Message,
                ScheduledAt   = req.ScheduledAt,
                IsSent        = false,
                CreatedAt     = DateTime.UtcNow
            };

            _db.PatientFollowUpReminders.Add(reminder);
            await _db.SaveChangesAsync();

            // If scheduled time is now or past, send immediately
            if (req.ScheduledAt <= DateTime.UtcNow)
            {
                await SendFollowUpAsync(reminder, patient);
            }

            return Ok(new { success = true, id = reminder.Id });
        }

        [HttpPost("follow-up-reminders/{id}/send")]
        public async Task<IActionResult> SendFollowUpNow(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reminder = await _db.PatientFollowUpReminders
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == userId);
            if (reminder == null) return NotFound();

            await SendFollowUpAsync(reminder, reminder.Patient!);
            return Ok(new { success = true });
        }

        [HttpDelete("follow-up-reminders/{id}")]
        public async Task<IActionResult> DeleteFollowUp(int id)
        {
            var userId = _userManager.GetUserId(User);
            var r = await _db.PatientFollowUpReminders
                .FirstOrDefaultAsync(x => x.Id == id && x.DoctorId == userId);
            if (r == null) return NotFound();
            _db.PatientFollowUpReminders.Remove(r);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3. VIRTUAL WAITING ROOM
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("waiting-room")]
        public async Task<IActionResult> GetWaitingRoom()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            var entries = await _db.WaitingRoomEntries
                .Include(e => e.Patient)
                .Include(e => e.Appointment)
                .Where(e => e.DoctorDbId == doctor.Id
                         && e.Status == "Waiting"
                         && e.JoinedAt.Date == DateTime.UtcNow.Date)
                .OrderBy(e => e.QueuePosition)
                .Select(e => new
                {
                    e.Id,
                    e.QueuePosition,
                    e.EstimatedWaitMinutes,
                    e.Status,
                    e.JoinedAt,
                    PatientId   = e.PatientId,
                    PatientName = e.Patient != null
                        ? e.Patient.FirstName + " " + e.Patient.LastName
                        : "Unknown",
                    PatientImage = e.Patient != null
                        ? (e.Patient.ProfileImage ?? "https://ui-avatars.com/api/?name=" + e.PatientId)
                        : "",
                    AppointmentType = e.Appointment != null ? e.Appointment.ConsultationType : "Consultation",
                    AppointmentId = e.AppointmentId
                })
                .ToListAsync();

            return Ok(new
            {
                TotalWaiting  = entries.Count,
                DoctorSlotMin = doctor.SlotDuration,
                Queue         = entries
            });
        }

        [HttpPost("waiting-room/join")]
        public async Task<IActionResult> JoinWaitingRoom([FromBody] dynamic body)
        {
            // Called by patient side — doctor's frontend shows them in queue
            var patientId     = (string)body.patientId;
            var appointmentId = (int)body.appointmentId;

            var appointment = await _db.Appointments.FindAsync(appointmentId);
            if (appointment == null) return NotFound();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == appointment.DoctorId);
            if (doctor == null) return NotFound();

            // Remove any stale entry for same patient+doctor today
            var stale = await _db.WaitingRoomEntries
                .Where(e => e.PatientId == patientId
                         && e.DoctorDbId == doctor.Id
                         && e.JoinedAt.Date == DateTime.UtcNow.Date)
                .ToListAsync();
            if (stale.Any()) _db.WaitingRoomEntries.RemoveRange(stale);

            int currentMax = await _db.WaitingRoomEntries
                .Where(e => e.DoctorDbId == doctor.Id
                         && e.Status == "Waiting"
                         && e.JoinedAt.Date == DateTime.UtcNow.Date)
                .Select(e => (int?)e.QueuePosition)
                .MaxAsync() ?? 0;

            int position = currentMax + 1;
            int wait     = (position - 1) * (doctor.SlotDuration + doctor.BufferTime);

            var entry = new WaitingRoomEntry
            {
                DoctorDbId            = doctor.Id,
                PatientId             = patientId,
                AppointmentId         = appointmentId,
                QueuePosition         = position,
                EstimatedWaitMinutes  = wait,
                Status                = "Waiting",
                JoinedAt              = DateTime.UtcNow
            };

            _db.WaitingRoomEntries.Add(entry);
            await _db.SaveChangesAsync();

            return Ok(new { position, estimatedWaitMinutes = wait, entryId = entry.Id });
        }

        [HttpPost("waiting-room/call")]
        public async Task<IActionResult> CallNextPatient([FromBody] WaitRoomCallRequest req)
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            var entry = await _db.WaitingRoomEntries
                .FirstOrDefaultAsync(e => e.Id == req.EntryId && e.DoctorDbId == doctor.Id);
            if (entry == null) return NotFound();

            entry.Status   = "Called";
            entry.CalledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify patient via push
            await _notificationService.NotifyUserAsync(
                entry.PatientId,
                NotificationType.General,
                "🩺 Doctor is Ready",
                "Your turn has arrived. Please join the consultation.",
                data: new Dictionary<string, string>
                {
                    { "type", "waiting_room_called" },
                    { "appointmentId", entry.AppointmentId.ToString() }
                });

            return Ok(new { success = true });
        }

        [HttpPost("waiting-room/done/{entryId}")]
        public async Task<IActionResult> MarkDone(int entryId)
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            var entry = await _db.WaitingRoomEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.DoctorDbId == doctor.Id);
            if (entry == null) return NotFound();

            entry.Status = "Done";
            await _db.SaveChangesAsync();

            // Recalculate wait times for remaining queue
            var remaining = await _db.WaitingRoomEntries
                .Where(e => e.DoctorDbId == doctor.Id
                         && e.Status == "Waiting"
                         && e.JoinedAt.Date == DateTime.UtcNow.Date)
                .OrderBy(e => e.QueuePosition)
                .ToListAsync();

            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].QueuePosition        = i + 1;
                remaining[i].EstimatedWaitMinutes = i * (doctor.SlotDuration + doctor.BufferTime);
            }
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 4. DOCTOR-TO-DOCTOR REFERRALS
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("referrals/sent")]
        public async Task<IActionResult> GetSentReferrals()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var referrals = await _db.DoctorReferrals
                .Include(r => r.ReceivingDoctor)
                .Include(r => r.Patient)
                .Where(r => r.ReferringDoctorUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Reason,
                    r.ClinicalNotes,
                    r.Status,
                    r.CreatedAt,
                    r.RespondedAt,
                    PatientName = r.Patient != null
                        ? r.Patient.FirstName + " " + r.Patient.LastName : "Unknown",
                    ReceiverName = r.ReceivingDoctor != null
                        ? r.ReceivingDoctor.FirstName + " " + r.ReceivingDoctor.LastName : "Unknown"
                })
                .ToListAsync();

            return Ok(referrals);
        }

        [HttpGet("referrals/received")]
        public async Task<IActionResult> GetReceivedReferrals()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var referrals = await _db.DoctorReferrals
                .Include(r => r.ReferringDoctor)
                .Include(r => r.Patient)
                .Where(r => r.ReceivingDoctorUserId == userId && r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Reason,
                    r.ClinicalNotes,
                    r.Status,
                    r.CreatedAt,
                    PatientId   = r.PatientId,
                    PatientName = r.Patient != null
                        ? r.Patient.FirstName + " " + r.Patient.LastName : "Unknown",
                    ReferrerName = r.ReferringDoctor != null
                        ? r.ReferringDoctor.FirstName + " " + r.ReferringDoctor.LastName : "Unknown"
                })
                .ToListAsync();

            return Ok(referrals);
        }

        [HttpPost("referrals")]
        public async Task<IActionResult> CreateReferral([FromBody] ReferralRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var receivingUser = await _userManager.FindByIdAsync(req.ReceivingDoctorUserId);
            if (receivingUser == null) return NotFound(new { message = "Receiving doctor not found" });

            var referral = new DoctorReferral
            {
                ReferringDoctorUserId  = userId,
                ReceivingDoctorUserId  = req.ReceivingDoctorUserId,
                PatientId              = req.PatientId,
                AppointmentId          = req.AppointmentId,
                Reason                 = req.Reason,
                ClinicalNotes          = req.ClinicalNotes,
                Status                 = "Pending",
                CreatedAt              = DateTime.UtcNow
            };

            _db.DoctorReferrals.Add(referral);
            await _db.SaveChangesAsync();

            // Notify receiving doctor
            await _notificationService.NotifyUserAsync(
                req.ReceivingDoctorUserId,
                NotificationType.General,
                "📋 New Patient Referral",
                $"You have received a new patient referral. Reason: {req.Reason}",
                data: new Dictionary<string, string>
                {
                    { "type", "referral_received" },
                    { "referralId", referral.Id.ToString() }
                });

            return Ok(new { success = true, referralId = referral.Id });
        }

        [HttpPost("referrals/respond")]
        public async Task<IActionResult> RespondToReferral([FromBody] ReferralRespondRequest req)
        {
            var userId = _userManager.GetUserId(User);
            var referral = await _db.DoctorReferrals
                .FirstOrDefaultAsync(r => r.Id == req.ReferralId && r.ReceivingDoctorUserId == userId);
            if (referral == null) return NotFound();

            referral.Status      = req.Status; // "Accepted" or "Declined"
            referral.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify referring doctor
            await _notificationService.NotifyUserAsync(
                referral.ReferringDoctorUserId,
                NotificationType.General,
                req.Status == "Accepted" ? "✅ Referral Accepted" : "❌ Referral Declined",
                $"Your referral has been {req.Status.ToLower()} by Dr. {(await _userManager.FindByIdAsync(userId))?.FullName}",
                data: new Dictionary<string, string>
                {
                    { "type", "referral_response" },
                    { "referralId", referral.Id.ToString() }
                });

            return Ok(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 5. CPD / CME CREDIT TRACKER
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("cpd")]
        public async Task<IActionResult> GetCpdActivities([FromQuery] int year = 0)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (year == 0) year = DateTime.UtcNow.Year;

            var activities = await _db.CpdActivities
                .Where(a => a.DoctorId == userId && a.ActivityDate.Year == year)
                .OrderByDescending(a => a.ActivityDate)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Provider,
                    a.ActivityType,
                    a.CreditPoints,
                    a.ActivityDate,
                    a.CertificateUrl,
                    a.Description,
                    a.IsVerified,
                    a.CreatedAt
                })
                .ToListAsync();

            var totalPoints = activities.Sum(a => a.CreditPoints);
            var goalPoints  = 20; // PMDC standard: 20 CPD per year

            return Ok(new
            {
                Year          = year,
                TotalPoints   = totalPoints,
                GoalPoints    = goalPoints,
                Progress      = Math.Min(100, (int)((double)totalPoints / goalPoints * 100)),
                Activities    = activities,
                ByType        = activities.GroupBy(a => a.ActivityType)
                    .Select(g => new { Type = g.Key, Points = g.Sum(x => x.CreditPoints), Count = g.Count() })
            });
        }

        [HttpPost("cpd")]
        public async Task<IActionResult> AddCpdActivity([FromBody] CpdActivityRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (req.Id == 0)
            {
                var activity = new CpdActivity
                {
                    DoctorId     = userId,
                    Title        = req.Title,
                    Provider     = req.Provider,
                    ActivityType = req.ActivityType,
                    CreditPoints = req.CreditPoints,
                    ActivityDate = req.ActivityDate,
                    Description  = req.Description,
                    IsVerified   = false,
                    CreatedAt    = DateTime.UtcNow
                };
                _db.CpdActivities.Add(activity);
                await _db.SaveChangesAsync();
                return Ok(new { success = true, id = activity.Id });
            }
            else
            {
                var activity = await _db.CpdActivities
                    .FirstOrDefaultAsync(a => a.Id == req.Id && a.DoctorId == userId);
                if (activity == null) return NotFound();

                activity.Title        = req.Title;
                activity.Provider     = req.Provider;
                activity.ActivityType = req.ActivityType;
                activity.CreditPoints = req.CreditPoints;
                activity.ActivityDate = req.ActivityDate;
                activity.Description  = req.Description;
                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
        }

        [HttpDelete("cpd/{id}")]
        public async Task<IActionResult> DeleteCpdActivity(int id)
        {
            var userId = _userManager.GetUserId(User);
            var a = await _db.CpdActivities
                .FirstOrDefaultAsync(x => x.Id == id && x.DoctorId == userId);
            if (a == null) return NotFound();
            _db.CpdActivities.Remove(a);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 6. CLINIC STAFF MANAGEMENT
        // ══════════════════════════════════════════════════════════════════════

        // Quick email test — call GET /api/doctor/test-email?to=someone@gmail.com
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromQuery] string? to)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var recipient = to ?? (await _userManager.FindByIdAsync(userId))?.Email;
            if (string.IsNullOrEmpty(recipient))
                return BadRequest(new { message = "Provide ?to=email@example.com" });

            try
            {
                await _emailSender.SendEmailAsync(
                    recipient,
                    "MedLink — Email Test ✅",
                    $"<h2>Email is working!</h2><p>Sent at {DateTime.UtcNow:u} UTC</p><p>If you see this, SMTP is configured correctly.</p>");

                return Ok(new { success = true, sentTo = recipient, message = "Test email sent! Check inbox." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    sentTo  = recipient,
                    error   = ex.Message,
                    detail  = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var staff = await _db.ClinicStaff
                .Where(s => s.DoctorId == userId && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Email,
                    s.Phone,
                    s.Role,
                    s.IsActive,
                    s.AddedAt,
                    s.StaffUserId
                })
                .ToListAsync();

            return Ok(staff);
        }

        [HttpPost("staff")]
        public async Task<IActionResult> AddStaff([FromBody] StaffAddRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Get doctor info for the email
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var doctorName = doctor?.Name ?? "Your Doctor";

            // Check if user already exists, create if not
            var existing = await _userManager.FindByEmailAsync(req.Email);
            string staffUserId;
            bool isNewUser = false;

            if (existing == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName       = req.Email,
                    Email          = req.Email,
                    Name           = req.Name,
                    FirstName      = req.Name.Split(' ').First(),
                    LastName       = req.Name.Contains(' ') ? req.Name[(req.Name.IndexOf(' ') + 1)..] : "",
                    PhoneNumber    = req.Phone,
                    EmailConfirmed = true,
                    ApprovalStatus = "Approved"
                };
                var result = await _userManager.CreateAsync(newUser, "Staff@12345!");
                if (!result.Succeeded)
                    return BadRequest(new { message = "Failed to create staff account", errors = result.Errors.Select(e => e.Description) });

                await _userManager.AddToRoleAsync(newUser, "Patient");
                staffUserId = newUser.Id;
                isNewUser   = true;
            }
            else
            {
                staffUserId = existing.Id;
            }

            var member = new ClinicStaff
            {
                DoctorId    = userId,
                StaffUserId = staffUserId,
                Name        = req.Name,
                Email       = req.Email,
                Phone       = req.Phone,
                Role        = req.Role,
                IsActive    = true,
                AddedAt     = DateTime.UtcNow
            };

            _db.ClinicStaff.Add(member);
            await _db.SaveChangesAsync();

            // Send welcome email (only for newly created accounts)
            string? emailError = null;
            if (isNewUser)
            {
                try
                {
                    var portalUrl = "https://medlinkwebportal-production.up.railway.app";
                    var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background:#f0f5fa; margin:0; padding:0; }}
    .wrap {{ max-width:580px; margin:40px auto; background:#fff; border-radius:20px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,.08); }}
    .top  {{ background:#1e293b; padding:36px 40px; text-align:center; }}
    .top h1 {{ color:#fff; margin:0; font-size:26px; letter-spacing:-0.5px; }}
    .top p  {{ color:#94a3b8; margin:8px 0 0; font-size:14px; }}
    .body {{ padding:36px 40px; }}
    .hi   {{ font-size:20px; font-weight:700; color:#0f172a; margin-bottom:12px; }}
    p     {{ color:#475569; line-height:1.7; margin:0 0 16px; }}
    .box  {{ background:#f8fafc; border:1px solid #e2e8f0; border-radius:14px; padding:20px 24px; margin:24px 0; }}
    .box .label {{ font-size:11px; font-weight:700; color:#94a3b8; text-transform:uppercase; letter-spacing:1px; margin-bottom:4px; }}
    .box .value {{ font-size:15px; font-weight:700; color:#0f172a; }}
    .btn  {{ display:inline-block; padding:14px 32px; background:#2563eb; color:#fff !important; border-radius:12px; font-weight:700; font-size:14px; text-decoration:none; margin-top:8px; }}
    .warn {{ font-size:12px; color:#f59e0b; margin-top:16px; }}
    .foot {{ background:#f8fafc; padding:20px 40px; text-align:center; color:#94a3b8; font-size:12px; border-top:1px solid #e2e8f0; }}
  </style>
</head>
<body>
<div class='wrap'>
  <div class='top'>
    <h1>MedLink Staff Access</h1>
    <p>You have been added to a clinic team</p>
  </div>
  <div class='body'>
    <div class='hi'>Welcome, {req.Name}! 👋</div>
    <p>Dr. <strong>{doctorName}</strong> has added you as a <strong>{req.Role}</strong> on MedLink Portal. Your account is ready — here are your login credentials:</p>

    <div class='box'>
      <div class='label'>Email (Login)</div>
      <div class='value'>{req.Email}</div>
    </div>
    <div class='box'>
      <div class='label'>Temporary Password</div>
      <div class='value' style='letter-spacing:2px;'>Staff@12345!</div>
    </div>

    <p class='warn'>⚠️ Please change your password immediately after your first login.</p>

    <p>As a {req.Role}, you can help manage appointments, the waiting room, and patient scheduling on behalf of Dr. {doctorName}.</p>

    <a href='{portalUrl}' class='btn'>Log In to MedLink Portal →</a>

    <p style='margin-top:24px; font-size:13px; color:#94a3b8;'>If you were not expecting this invitation, please ignore this email.</p>
  </div>
  <div class='foot'>
    MedLink Portal &bull; Integrated Health Systems &bull; {DateTime.UtcNow.Year}<br>
    <a href='{portalUrl}' style='color:#2563eb;'>{portalUrl}</a>
  </div>
</div>
</body>
</html>";

                    await _emailSender.SendEmailAsync(
                        req.Email,
                        $"Welcome to MedLink — You've been added as {req.Role} by Dr. {doctorName}",
                        html);
                }
                catch (Exception ex)
                {
                    // Staff was created successfully; just note email issue
                    emailError = ex.Message;
                }
            }

            return Ok(new
            {
                success    = true,
                staffId    = member.Id,
                userId     = staffUserId,
                emailSent  = isNewUser && emailError == null,
                emailError = emailError
            });
        }

        [HttpDelete("staff/{id}")]
        public async Task<IActionResult> RemoveStaff(int id)
        {
            var userId = _userManager.GetUserId(User);
            var member = await _db.ClinicStaff
                .FirstOrDefaultAsync(s => s.Id == id && s.DoctorId == userId);
            if (member == null) return NotFound();

            member.IsActive = false; // Soft delete
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("staff/{id}/resend-email")]
        public async Task<IActionResult> ResendStaffEmail(int id)
        {
            var userId = _userManager.GetUserId(User);
            var member = await _db.ClinicStaff
                .FirstOrDefaultAsync(s => s.Id == id && s.DoctorId == userId && s.IsActive);
            if (member == null) return NotFound();

            var doctor     = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var doctorName = doctor?.Name ?? "Your Doctor";
            var portalUrl  = "https://medlinkwebportal-production.up.railway.app";

            var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background:#f0f5fa; margin:0; padding:0; }}
    .wrap {{ max-width:580px; margin:40px auto; background:#fff; border-radius:20px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,.08); }}
    .top  {{ background:#1e293b; padding:36px 40px; text-align:center; }}
    .top h1 {{ color:#fff; margin:0; font-size:26px; letter-spacing:-0.5px; }}
    .top p  {{ color:#94a3b8; margin:8px 0 0; font-size:14px; }}
    .body {{ padding:36px 40px; }}
    .hi   {{ font-size:20px; font-weight:700; color:#0f172a; margin-bottom:12px; }}
    p     {{ color:#475569; line-height:1.7; margin:0 0 16px; }}
    .box  {{ background:#f8fafc; border:1px solid #e2e8f0; border-radius:14px; padding:20px 24px; margin:24px 0; }}
    .box .label {{ font-size:11px; font-weight:700; color:#94a3b8; text-transform:uppercase; letter-spacing:1px; margin-bottom:4px; }}
    .box .value {{ font-size:15px; font-weight:700; color:#0f172a; }}
    .btn  {{ display:inline-block; padding:14px 32px; background:#2563eb; color:#fff !important; border-radius:12px; font-weight:700; font-size:14px; text-decoration:none; margin-top:8px; }}
    .warn {{ font-size:12px; color:#f59e0b; margin-top:16px; }}
    .foot {{ background:#f8fafc; padding:20px 40px; text-align:center; color:#94a3b8; font-size:12px; border-top:1px solid #e2e8f0; }}
  </style>
</head>
<body>
<div class='wrap'>
  <div class='top'>
    <h1>MedLink Staff Access</h1>
    <p>Your login credentials — resent by Dr. {doctorName}</p>
  </div>
  <div class='body'>
    <div class='hi'>Hello, {member.Name}! 👋</div>
    <p>Here are your MedLink Portal login credentials for Dr. <strong>{doctorName}</strong>'s clinic:</p>
    <div class='box'>
      <div class='label'>Email (Login)</div>
      <div class='value'>{member.Email}</div>
    </div>
    <div class='box'>
      <div class='label'>Temporary Password</div>
      <div class='value' style='letter-spacing:2px;'>Staff@12345!</div>
    </div>
    <p class='warn'>⚠️ Please change your password after logging in.</p>
    <a href='{portalUrl}' class='btn'>Log In to MedLink Portal →</a>
  </div>
  <div class='foot'>MedLink Portal &bull; Integrated Health Systems</div>
</div>
</body>
</html>";

            try
            {
                await _emailSender.SendEmailAsync(
                    member.Email,
                    $"Your MedLink Staff Login — Dr. {doctorName}",
                    html);
                return Ok(new { success = true, message = "Credentials email resent." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Email failed: {ex.Message}" });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 7. PATIENT REVIEW REPLIES
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            var reviews = await _db.Reviews
                .Include(r => r.Doctor)
                .Where(r => r.DoctorId == doctor.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reviewIds = reviews.Select(r => r.Id).ToList();
            var replies = await _db.ReviewReplies
                .Where(rp => reviewIds.Contains(rp.ReviewId))
                .ToDictionaryAsync(rp => rp.ReviewId, rp => rp.ReplyText);

            var patients = await _userManager.Users
                .Where(u => reviews.Select(r => r.PatientId).Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

            var result = reviews.Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                PatientName = patients.TryGetValue(r.PatientId ?? "", out var pn) ? pn : "Anonymous",
                DoctorReply = replies.TryGetValue(r.Id, out var reply) ? reply : null
            });

            return Ok(result);
        }

        [HttpPost("reviews/reply")]
        public async Task<IActionResult> ReplyToReview([FromBody] ReviewReplyRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            // Verify review belongs to this doctor
            var review = await _db.Reviews
                .FirstOrDefaultAsync(r => r.Id == req.ReviewId && r.DoctorId == doctor.Id);
            if (review == null) return NotFound();

            // Update or create reply
            var existing = await _db.ReviewReplies
                .FirstOrDefaultAsync(rp => rp.ReviewId == req.ReviewId);

            if (existing != null)
            {
                existing.ReplyText = req.ReplyText;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.ReviewReplies.Add(new ReviewReply
                {
                    ReviewId   = req.ReviewId,
                    DoctorId   = userId,
                    ReplyText  = req.ReplyText,
                    CreatedAt  = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 8. PEAK HOURS ANALYTICS
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("analytics/peak-hours")]
        public async Task<IActionResult> GetPeakHours()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            var appointments = await _db.Appointments
                .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate >= DateTime.UtcNow.AddDays(-90))
                .ToListAsync();

            // Hourly distribution
            var byHour = appointments
                .GroupBy(a => a.AppointmentDate.Hour)
                .Select(g => new
                {
                    Hour  = g.Key,
                    Label = $"{g.Key:00}:00",
                    Count = g.Count()
                })
                .OrderBy(x => x.Hour)
                .ToList();

            // Day of week distribution
            var byDay = appointments
                .GroupBy(a => a.AppointmentDate.DayOfWeek)
                .Select(g => new
                {
                    Day   = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderBy(x => x.Day)
                .ToList();

            // Monthly trend (last 6 months)
            var now       = DateTime.UtcNow;
            var sixMonths = now.AddMonths(-6);
            var monthly   = appointments
                .Where(a => a.AppointmentDate >= sixMonths)
                .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yy"),
                    Count = g.Count(),
                    Revenue = (decimal)g.Count() * 1500 // Approximate
                })
                .OrderBy(x => x.Month)
                .ToList();

            // Peak hour + day
            var peakHour = byHour.OrderByDescending(x => x.Count).FirstOrDefault();
            var peakDay  = byDay.OrderByDescending(x => x.Count).FirstOrDefault();

            return Ok(new
            {
                ByHour     = byHour,
                ByDay      = byDay,
                Monthly    = monthly,
                TotalLast90 = appointments.Count,
                PeakHour   = peakHour,
                PeakDay    = peakDay,
                AvgPerDay  = appointments.Count > 0
                    ? Math.Round((double)appointments.Count / 90, 1) : 0
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 10. PATIENT HEALTH REPORT PDF (Doctor-Generated)
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("patient-report/{patientId}")]
        public async Task<IActionResult> GetPatientReportData(string patientId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            // Verify doctor has treated this patient
            var hasRelation = await _db.Appointments
                .AnyAsync(a => a.DoctorId == doctor.Id && a.UserId == patientId);
            if (!hasRelation) return Forbid();

            var patient = await _userManager.FindByIdAsync(patientId);
            if (patient == null) return NotFound();

            var appointments = await _db.Appointments
                .Where(a => a.UserId == patientId && a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(20)
                .Select(a => new
                {
                    Date   = a.AppointmentDate,
                    Type   = a.ConsultationType,
                    Status = a.Status,
                    a.Id
                })
                .ToListAsync();

            var prescriptions = await _db.Prescriptions
                .Include(p => p.PrescriptionMedicines)
                    .ThenInclude(pm => pm.Medicine)
                .Where(p => p.PatientId == patientId && p.DoctorId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    p.Diagnosis,
                    p.Notes,
                    p.CreatedAt,
                    Medicines = p.PrescriptionMedicines.Select(pm => new
                    {
                        pm.Medicine.Name,
                        pm.Dosage,
                        pm.Frequency,
                        pm.Duration
                    })
                })
                .ToListAsync();

            var vitals = await _db.HealthVitals
                .Where(v => v.UserId == patientId)
                .OrderByDescending(v => v.Timestamp)
                .Take(10)
                .Select(v => new
                {
                    v.VitalType,
                    v.Value,
                    v.Unit,
                    RecordedAt = v.Timestamp
                })
                .ToListAsync();

            var labResults = await _db.LabTestResults
                .Include(r => r.LabBooking)
                .Where(r => r.LabBooking.PatientId == patientId)
                .OrderByDescending(r => r.UploadedDate)
                .Take(5)
                .Select(r => new
                {
                    r.ReportUrl,
                    r.UploadedDate,
                    r.LabBookingId
                })
                .ToListAsync();

            return Ok(new
            {
                Patient = new
                {
                    Id      = patientId,
                    Name    = patient.FirstName + " " + patient.LastName,
                    Phone   = patient.PhoneNumber,
                    Gender  = patient.Gender,
                    DateOfBirth = patient.DateOfBirth
                },
                Doctor = new
                {
                    Id        = doctor.Id,
                    Name      = doctor.Name,
                    Specialty = doctor.Specialty,
                    PMDC      = doctor.PmdcRegistrationNumber
                },
                Appointments   = appointments,
                Prescriptions  = prescriptions,
                Vitals         = vitals,
                LabResults     = labResults,
                GeneratedAt    = DateTime.UtcNow
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // 11. CONSULTATION SUMMARY EMAIL
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost("consultation-summary/{appointmentId}")]
        public async Task<IActionResult> SendConsultationSummary(int appointmentId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            var appointment = await _db.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctor.Id);
            if (appointment == null) return NotFound();

            var patient = appointment.Patient;
            if (patient == null || string.IsNullOrEmpty(patient.Email))
                return BadRequest(new { message = "Patient email not available" });

            var prescription = await _db.Prescriptions
                .Include(p => p.PrescriptionMedicines)
                    .ThenInclude(pm => pm.Medicine)
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

            // Build HTML email
            var sb = new StringBuilder();
            sb.AppendLine($@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', sans-serif; background: #f0f5fa; margin: 0; padding: 0; }}
    .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,.08); }}
    .header {{ background: #2563EB; padding: 32px 40px; }}
    .header h1 {{ color: white; margin: 0; font-size: 24px; }}
    .header p {{ color: rgba(255,255,255,.8); margin: 8px 0 0; }}
    .body {{ padding: 32px 40px; }}
    .section {{ margin-bottom: 28px; }}
    .section h3 {{ color: #1e293b; font-size: 14px; font-weight: 800; letter-spacing: 1px; text-transform: uppercase; margin: 0 0 16px; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px; }}
    .pill {{ display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 700; background: #eff6ff; color: #2563eb; margin: 2px 4px 2px 0; }}
    .med-row {{ background: #f8fafc; border-radius: 10px; padding: 12px 16px; margin-bottom: 8px; }}
    .med-name {{ font-weight: 800; color: #0f172a; }}
    .med-detail {{ color: #64748b; font-size: 13px; }}
    .footer {{ background: #f8fafc; padding: 24px 40px; text-align: center; color: #94a3b8; font-size: 12px; }}
    .badge {{ display: inline-block; padding: 6px 16px; background: #ecfdf5; color: #10b981; border-radius: 20px; font-size: 12px; font-weight: 700; }}
  </style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <h1>MedLink — Consultation Summary</h1>
    <p>{appointment.AppointmentDate:dddd, MMMM dd, yyyy} &nbsp;|&nbsp; {appointment.ConsultationType}</p>
  </div>
  <div class='body'>
    <div class='section'>
      <h3>Patient</h3>
      <p><strong>{patient.FirstName} {patient.LastName}</strong></p>
    </div>
    <div class='section'>
      <h3>Attending Doctor</h3>
      <p><strong>Dr. {doctor.Name}</strong> &nbsp; <span class='badge'>{doctor.Specialty}</span></p>
      {(string.IsNullOrEmpty(doctor.PmdcRegistrationNumber) ? "" : $"<p style='color:#64748b;font-size:13px;'>PMDC: {doctor.PmdcRegistrationNumber}</p>")}
    </div>");

            if (prescription != null)
            {
                sb.AppendLine($@"
    <div class='section'>
      <h3>Diagnosis</h3>
      <p>{prescription.Diagnosis}</p>
    </div>");
                if (prescription.PrescriptionMedicines.Any())
                {
                    sb.AppendLine("<div class='section'><h3>Prescribed Medications</h3>");
                    foreach (var pm in prescription.PrescriptionMedicines)
                    {
                        sb.AppendLine($@"<div class='med-row'>
          <div class='med-name'>{pm.Medicine?.Name ?? "Medicine"}</div>
          <div class='med-detail'>{pm.Dosage} &bull; {pm.Frequency} &bull; {pm.Duration}</div>
        </div>");
                    }
                    sb.AppendLine("</div>");
                }
                if (!string.IsNullOrEmpty(prescription.Notes))
                {
                    sb.AppendLine($@"<div class='section'><h3>Doctor's Notes</h3><p>{prescription.Notes}</p></div>");
                }
            }

            sb.AppendLine($@"
    <div class='section'>
      <h3>Next Steps</h3>
      <p style='color:#64748b;'>Please follow the prescribed medications and visit again if symptoms persist. For any concerns, message your doctor directly through the MedLink app.</p>
    </div>
  </div>
  <div class='footer'>
    <p>This summary was generated by MedLink &bull; For information only &bull; Not a medical certificate</p>
    <p style='margin-top:8px;'>medlinkwebportal-production.up.railway.app</p>
  </div>
</div>
</body>
</html>");

            try
            {
                await _emailSender.SendEmailAsync(
                    patient.Email,
                    $"Your Consultation Summary — Dr. {doctor.Name} | MedLink",
                    sb.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Email failed: {ex.Message}" });
            }

            return Ok(new { success = true, message = "Consultation summary sent to patient." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helper
        // ─────────────────────────────────────────────────────────────────────
        private async Task SendFollowUpAsync(PatientFollowUpReminder reminder, ApplicationUser patient)
        {
            if (patient == null || string.IsNullOrEmpty(patient.Email)) return;

            // Push notification
            await _notificationService.NotifyUserAsync(
                reminder.PatientId,
                NotificationType.AppointmentReminder,
                "💊 Follow-up Reminder",
                reminder.Message,
                data: new Dictionary<string, string>
                {
                    { "type", "follow_up_reminder" },
                    { "reminderId", reminder.Id.ToString() }
                });

            // Email
            try
            {
                await _emailSender.SendEmailAsync(
                    patient.Email,
                    "Follow-up Reminder from MedLink",
                    $"<p>Dear {patient.FirstName},</p><p>{reminder.Message}</p><p>Please book your follow-up appointment at your earliest convenience.</p><p>MedLink Team</p>");
            }
            catch { /* non-fatal */ }

            reminder.IsSent = true;
            reminder.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}

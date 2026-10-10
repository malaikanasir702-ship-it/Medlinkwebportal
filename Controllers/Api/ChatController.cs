using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/chat")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Check whether the current user can chat with the given recipient.
        /// Returns canChat, status, appointmentDate, appointmentTime, and message.
        /// </summary>
        [HttpGet("check-access")]
        public async Task<IActionResult> CheckAccess([FromQuery] string recipientId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                if (string.IsNullOrEmpty(recipientId)) return BadRequest(new { canChat = false, status = "error", message = "Recipient ID is required." });

                var today = DateTime.UtcNow.Date;

                // Find the most relevant appointment between these two users
                // (patient UserId <-> doctor UserId via Doctor table)
                var matchingAppointments = await _context.Appointments
                    .Where(a =>
                        a.Status != "Cancelled" && a.Status != "Rejected" &&
                        (
                            (a.UserId == userId && _context.Doctors.Any(d => d.Id == a.DoctorId && d.UserId == recipientId)) ||
                            (a.UserId == recipientId && _context.Doctors.Any(d => d.Id == a.DoctorId && d.UserId == userId))
                        )
                    )
                    .ToListAsync();

                var appointment = matchingAppointments.FirstOrDefault(a => a.AppointmentDate.Date == today)
                                ?? matchingAppointments.Where(a => a.AppointmentDate.Date > today).OrderBy(a => a.AppointmentDate).FirstOrDefault()
                                ?? matchingAppointments.OrderByDescending(a => a.AppointmentDate).FirstOrDefault();

                if (appointment == null)
                {
                    return Ok(new
                    {
                        canChat = false,
                        status = "no_appointment",
                        appointmentDate = (string?)null,
                        appointmentTime = (string?)null,
                        message = "You need to book an appointment first to start a chat."
                    });
                }

                if (appointment.AppointmentDate.Date == today)
                {
                    return Ok(new
                    {
                        canChat = true,
                        status = "active_today",
                        appointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                        appointmentTime = appointment.TimeSlot,
                        message = "Chat is active."
                    });
                }

                if (appointment.AppointmentDate.Date > today)
                {
                    return Ok(new
                    {
                        canChat = false,
                        status = "upcoming",
                        appointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                        appointmentTime = appointment.TimeSlot,
                        message = $"Chat will be active on your appointment day ({appointment.AppointmentDate:MMM dd, yyyy})."
                    });
                }

                // Past appointment
                return Ok(new
                {
                    canChat = false,
                    status = "past",
                    appointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                    appointmentTime = appointment.TimeSlot,
                    message = "Your appointment has passed. Book a new appointment to chat."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { canChat = false, status = "error", message = "Failed to check chat access." });
            }
        }

        [HttpGet("history/{recipientId}")]
        public async Task<IActionResult> GetHistory(string recipientId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var messages = await _context.ChatMessages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == recipientId) || 
                                (m.SenderId == recipientId && m.ReceiverId == userId))
                    .OrderByDescending(m => m.Timestamp)
                    .Take(50)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new {
                        m.Id,
                        m.SenderId,
                        m.Content,
                        m.Timestamp,
                        Type = m.MessageType.ToLower(),
                        m.AttachmentUrl,
                        m.AttachmentName,
                        m.AttachmentType,
                        m.IsDeleted,
                        m.DeletedBy
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load chat history." });
            }
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetChatRooms()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var today = DateTime.UtcNow.Date;

                // Get all doctors with whom the patient has appointments
                var appointments = await _context.Appointments
                    .Where(a => a.UserId == userId && a.Status != "Cancelled" && a.Status != "Rejected")
                    .Include(a => a.Doctor)
                    .ToListAsync();

                if (!appointments.Any())
                    return Ok(new List<object>());

                var grouped = appointments
                    .Where(a => a.Doctor != null && !string.IsNullOrEmpty(a.Doctor.UserId))
                    .GroupBy(a => a.Doctor.UserId)
                    .ToList();

                var doctorUserIds = grouped.Select(g => g.Key).ToList();
                var users = await _userManager.Users
                    .Where(u => doctorUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                var result = grouped.Select(g => {
                    var doctorUser = users.ContainsKey(g.Key) ? users[g.Key] : null;
                    var firstAppt = g.First();
                    var doctor = firstAppt.Doctor;
                    var hasToday = g.Any(x => x.AppointmentDate.Date == today);
                    var nextAppt = g.Where(x => x.AppointmentDate.Date >= today)
                                    .OrderBy(x => x.AppointmentDate)
                                    .FirstOrDefault();

                    var name = doctor?.Name ?? doctorUser?.FullName ?? doctorUser?.UserName ?? "Doctor";
                    var image = doctor?.Image ?? doctorUser?.ProfileImage ?? ("https://picsum.photos/seed/" + g.Key + "/100/100");
                    var specialty = doctor?.Specialty ?? "Specialist";

                    return new {
                        Id = g.Key,
                        ParticipantName = name,
                        ParticipantImage = image,
                        Specialty = specialty,
                        LastMessage = hasToday ? "Appointment is today. Chat is active." : "Chat active on appointment day.",
                        LastMessageTime = DateTime.UtcNow,
                        CanChatToday = hasToday,
                        NextAppointmentDate = nextAppt?.AppointmentDate.ToString("yyyy-MM-dd"),
                        NextAppointmentTime = nextAppt?.TimeSlot
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load chat rooms: " + ex.Message });
            }
        }
    }
}

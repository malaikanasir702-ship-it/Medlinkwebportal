using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Areas.Doctor.Models;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<MedLinkPortal.Hubs.ChatHub> _hubContext;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, IHubContext<MedLinkPortal.Hubs.ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string otherUserId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new {
                    m.Id,
                    m.Content,
                    m.SenderId,
                    m.Timestamp,
                    AttachmentPath = m.AttachmentUrl,
                    m.AttachmentType,
                    m.MessageType,
                    m.IsRead,
                    IsMe = m.SenderId == currentUserId
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string content, IFormFile? attachment)
        {
            var senderId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(senderId)) return Unauthorized();

            var now = DateTime.UtcNow;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == senderId);

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                DoctorId = doctor?.Id,
                Content = content ?? "",
                Timestamp = now,
                MessageType = "Text"
            };

            if (attachment != null && attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "chat");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(attachment.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(fileStream);
                }

                message.AttachmentUrl = "/uploads/chat/" + uniqueFileName;
                message.AttachmentType = attachment.ContentType.StartsWith("image/") ? "image" : "file";
                message.AttachmentName = attachment.FileName;
                message.MessageType = "Document";
                
                if (string.IsNullOrEmpty(message.Content))
                {
                    message.Content = message.AttachmentType == "image" ? "Sent an image" : $"Sent a file: {attachment.FileName}";
                }
            }

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            
            var senderName = User.Identity?.Name?.Split('@')[0] ?? "Doctor";
            var type = message.AttachmentType?.ToLower() ?? "text";
            
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", senderName, message.Content ?? "", type, message.AttachmentUrl, message.AttachmentName, senderId);
            await _hubContext.Clients.User(senderId).SendAsync("ReceiveMessage", senderName, message.Content ?? "", type, message.AttachmentUrl, message.AttachmentName, senderId);

            return Json(new { success = true, messageId = message.Id });
        }

        [HttpPost]
        public async Task<IActionResult> LogCall(string patientId, string callType)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == currentUserId);
            
            var message = new ChatMessage
            {
                SenderId = currentUserId,
                ReceiverId = patientId,
                DoctorId = doctor?.Id,
                MessageType = "Call",
                Content = $"{(callType == "video" ? "Video" : "Voice")} call started at {DateTime.UtcNow.AddHours(5):HH:mm}",
                Timestamp = DateTime.UtcNow,
                IsRead = true
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var senderName = User.Identity?.Name?.Split('@')[0] ?? "Doctor";
            await _hubContext.Clients.User(patientId).SendAsync("ReceiveMessage", senderName, message.Content, "Call", null, null, currentUserId);
            
            return Json(new { success = true, message = message });
        }
    }
}

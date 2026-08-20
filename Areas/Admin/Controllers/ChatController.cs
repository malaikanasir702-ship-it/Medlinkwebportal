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
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
        public async Task<IActionResult> Messages(string? patientId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // 1. Get IDs of all users who have messaged this Admin
            var usersWithMessagesIds = await _context.ChatMessages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            // 2. Get all patients
            var patients = await _userManager.GetUsersInRoleAsync("Patient");
            var patientIds = patients.Select(p => p.Id).ToList();

            // 3. Combine IDs
            var allContactIds = usersWithMessagesIds.Union(patientIds).Distinct().ToList();

            // 4. Fetch User objects
            var allContacts = await _userManager.Users
                .Where(u => allContactIds.Contains(u.Id))
                .ToListAsync();

            // 5. Enhance contacts with last message timestamp for sorting
            var contactsWithTime = new List<(ApplicationUser User, DateTime LastMessageTime)>();
            foreach (var contact in allContacts)
            {
                var lastMsgTime = await _context.ChatMessages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == contact.Id) ||
                                (m.SenderId == contact.Id && m.ReceiverId == userId))
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => m.Timestamp)
                    .FirstOrDefaultAsync();
                
                contactsWithTime.Add((contact, lastMsgTime == default ? DateTime.MinValue : lastMsgTime));
            }

            // Sort by most recent first, then by name
            var sortedContacts = contactsWithTime
                .OrderByDescending(c => c.LastMessageTime)
                .ThenBy(c => c.User.Name ?? c.User.Email)
                .Select(c => c.User)
                .ToList();

            if (!sortedContacts.Any())
            {
                sortedContacts = await _userManager.Users.Take(50).ToListAsync();
            }

            ViewBag.SelectedPatientId = patientId;
            return View(sortedContacts);
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

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
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
                    message.Content = "[Attachment: " + attachment.FileName + "]";
                }
            }

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            
            var senderName = "Admin";
            var type = message.AttachmentType?.ToLower() ?? "text";
            
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", senderName, message.Content, type, message.AttachmentUrl, message.AttachmentName, senderId);
            await _hubContext.Clients.User(senderId).SendAsync("ReceiveMessage", senderName, message.Content, type, message.AttachmentUrl, message.AttachmentName, senderId);

            return Json(new { success = true, messageId = message.Id });
        }
    }
}

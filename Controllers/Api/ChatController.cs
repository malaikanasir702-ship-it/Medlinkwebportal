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

                var sentTo = await _context.ChatMessages
                    .Where(m => m.SenderId == userId)
                    .Select(m => m.ReceiverId)
                    .Distinct()
                    .ToListAsync();

                var receivedFrom = await _context.ChatMessages
                    .Where(m => m.ReceiverId == userId)
                    .Select(m => m.SenderId)
                    .Distinct()
                    .ToListAsync();

                var participantIds = sentTo.Union(receivedFrom).ToList();

                var rooms = await _userManager.Users
                    .Where(u => participantIds.Contains(u.Id))
                    .Select(u => new {
                        Id = u.Id,
                        ParticipantName = u.FullName ?? u.UserName,
                        ParticipantImage = u.ProfileImage ?? "https://picsum.photos/seed/" + u.Id + "/100/100",
                        LastMessage = "Click to view chat",
                        LastMessageTime = DateTime.UtcNow
                    })
                    .ToListAsync();

                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load chat rooms." });
            }
        }
    }
}

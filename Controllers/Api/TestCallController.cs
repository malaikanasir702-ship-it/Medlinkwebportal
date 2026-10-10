using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Models;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestCallController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestCallController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet("send/{userId}")]
        public async Task<IActionResult> SendTestCall(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // Try finding by email if ID fails
                user = await _userManager.FindByEmailAsync(userId);
                if (user == null) return NotFound("User not found");
            }

            // Trigger the call notification
            await _notificationService.NotifyCallAsync(
                user.Id, 
                "test-sender-id", 
                "Test Doctor", 
                "video", 
                "test-sdp-data"
            );

            return Ok(new { 
                message = $"Test call notification sent to {user.Email} (ID: {user.Id})",
                fcmToken = string.IsNullOrEmpty(user.FcmToken) ? "Warning: User has no FCM token in DB!" : "Token exists"
            });
        }
    }
}

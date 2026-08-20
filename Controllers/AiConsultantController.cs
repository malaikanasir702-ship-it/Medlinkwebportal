using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Models;
using MedLinkPortal.Services;

namespace MedLinkPortal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")] // Require JWT for Flutter app
    public class AiConsultantController : ControllerBase
    {
        private readonly IAiChatService _aiChatService;

        public AiConsultantController(IAiChatService aiChatService)
        {
            _aiChatService = aiChatService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (string.IsNullOrEmpty(request.Message))
                return BadRequest("Message cannot be empty");

            var response = await _aiChatService.ProcessMessageAsync(userId, request.Message);
            return Ok(response);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _aiChatService.GetChatHistoryAsync(userId);
            return Ok(history);
        }

        [HttpDelete("history")]
        public async Task<IActionResult> ClearHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _aiChatService.ClearHistoryAsync(userId);
            return Ok(new { message = "History cleared" });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}

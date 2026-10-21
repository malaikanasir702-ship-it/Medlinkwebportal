using MedLinkPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Areas.Identity.Pages.Account;
using System.Security.Claims;

namespace MedLinkPortal.Controllers.Api
{
    /// <summary>
    /// Issues short-lived Agora RTC tokens so the client never needs the App Certificate.
    /// GET /api/agora/token?channelName=room_42&uid=0
    /// </summary>
    [ApiController]
    [Route("api/agora")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class AgoraController : ControllerBase
    {
        private readonly AgoraTokenService _agora;
        private readonly IConfiguration _config;

        public AgoraController(AgoraTokenService agora, IConfiguration config)
        {
            _agora = agora;
            _config = config;
        }

        /// <summary>
        /// Returns a short-lived RTC token + App ID for a channel join.
        /// channelName — e.g. "room_42" where 42 is appointmentId
        /// uid          — numeric UID (0 = let Agora assign)
        /// </summary>
        [HttpGet("token")]
        public IActionResult GetToken([FromQuery] string channelName, [FromQuery] uint uid = 0)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return BadRequest(new { success = false, message = "channelName is required." });

            try
            {
                var token = _agora.GenerateRtcToken(channelName, uid);
                var appId = Environment.GetEnvironmentVariable("AGORA_APP_ID")
                         ?? _config["Agora:AppId"]
                         ?? string.Empty;

                return Ok(new
                {
                    success = true,
                    token,
                    appId,
                    channelName,
                    uid,
                    expiresInSeconds = 3600
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Token generation failed.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mobile-friendly token endpoint — uses the authenticated user's ID as UID seed.
        /// POST /api/agora/join { appointmentId: 42, callType: "video" }
        /// </summary>
        [HttpPost("join")]
        public IActionResult JoinChannel([FromBody] AgoraJoinRequest req)
        {
            if (req == null || req.AppointmentId <= 0)
                return BadRequest(new { success = false, message = "Invalid request." });

            try
            {
                var channelName = $"room_{req.AppointmentId}";
                // Derive a stable numeric UID from the user's claim hash (keeps it consistent per user)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? "0";
                var uid = (uint)(Math.Abs(userId.GetHashCode()) % 100000 + 1);

                var token = _agora.GenerateRtcToken(channelName, uid);
                var appId = Environment.GetEnvironmentVariable("AGORA_APP_ID")
                         ?? _config["Agora:AppId"]
                         ?? string.Empty;

                return Ok(new
                {
                    success = true,
                    token,
                    appId,
                    channelName,
                    uid,
                    callType = req.CallType ?? "video",
                    expiresInSeconds = 3600
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { success = false, message = ex.Message });
            }
        }
    }

    public class AgoraJoinRequest
    {
        public int AppointmentId { get; set; }
        public string? CallType { get; set; } = "video";
    }
}

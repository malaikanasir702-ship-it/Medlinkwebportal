using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MedLinkPortal.Areas.Identity.Pages.Account;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.Features;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/ai-consultant-stream")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AiConsultantStreamController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly string _medLinkAiBaseUrl;

        public AiConsultantStreamController(IHttpClientFactory httpClientFactory, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _context = context;
            _medLinkAiBaseUrl = configuration["MedLinkAI:BaseUrl"]?.TrimEnd('/') ?? "http://127.0.0.1:8000";
        }

        [HttpPost("ask")]
        public async Task Ask([FromBody] JsonElement request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) { Response.StatusCode = 401; return; }

            var query = request.GetProperty("query").GetString();
            if (string.IsNullOrEmpty(query)) { Response.StatusCode = 400; return; }

            // 1. Fetch History from Database (Last 10 messages)
            var history = await _context.AiChatMessages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(10)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { role = m.Role, content = m.Content })
                .ToListAsync();

            var user = await _userManager.FindByIdAsync(userId);
            var client = _httpClientFactory.CreateClient("MedLinkAI");
            
            var pythonPayload = new
            {
                user_id = userId,
                user_name = user?.FirstName ?? "User",
                role = !string.IsNullOrEmpty(user?.Specialist) ? "Doctor" : "Patient",
                query = query,
                history = history
            };

            var content = new StringContent(JsonSerializer.Serialize(pythonPayload), Encoding.UTF8, "application/json");

            try
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, $"{_medLinkAiBaseUrl}/ask") { Content = content };
                using var response = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);
                
                if (response.IsSuccessStatusCode)
                {
                    Response.HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
                    Response.ContentType = "text/plain; charset=utf-8";
                    var fullResponse = new StringBuilder();
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);

                    // Use 1-char buffer for absolute minimum latency (instant streaming)
                    char[] buffer = new char[1];
                    int bytesRead;
                    while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        var chunk = new string(buffer, 0, bytesRead);
                        fullResponse.Append(chunk);
                        await Response.WriteAsync(chunk);
                        await Response.Body.FlushAsync();
                    }

                    // 2. Save current interaction to History
                    _context.AiChatMessages.Add(new AiChatMessage { UserId = userId, Role = "user", Content = query });
                    _context.AiChatMessages.Add(new AiChatMessage { UserId = userId, Role = "assistant", Content = fullResponse.ToString() });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    Response.StatusCode = (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                    await Response.WriteAsync($"Error: {ex.Message}");
                }
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var history = await _context.AiChatMessages
                    .Where(m => m.UserId == userId)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load AI chat history." });
            }
        }
    }
}

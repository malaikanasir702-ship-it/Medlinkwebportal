using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace MedLinkPortal.Middleware
{
    public class SessionTrackingMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionTrackingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, SignInManager<ApplicationUser> signInManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var sessionId = context.Request.Cookies["MedLink_SessionId"];

                    if (string.IsNullOrEmpty(sessionId))
                    {
                        sessionId = Guid.NewGuid().ToString();
                        context.Response.Cookies.Append("MedLink_SessionId", sessionId, new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddDays(30),
                            HttpOnly = true,
                            Secure = true,
                            IsEssential = true
                        });
                    }

                    var session = await dbContext.UserSessions
                        .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionIdentifier == sessionId);

                    if (session == null)
                    {
                        var userAgent = context.Request.Headers["User-Agent"].ToString();
                        session = new UserSession
                        {
                            UserId = userId,
                            SessionIdentifier = sessionId,
                            UserAgent = userAgent,
                            IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                            DeviceName = GetDeviceName(userAgent),
                            Location = "Unknown",
                            LoginTime = DateTime.UtcNow,
                            LastSeen = DateTime.UtcNow
                        };
                        dbContext.UserSessions.Add(session);
                    }
                    else
                    {
                        if (session.IsRevoked)
                        {
                            await signInManager.SignOutAsync();
                            context.Response.Cookies.Delete("MedLink_SessionId");
                            context.Response.Redirect("/Identity/Account/Login");
                            return;
                        }
                        session.LastSeen = DateTime.UtcNow;
                        session.IPAddress = context.Connection.RemoteIpAddress?.ToString();
                        session.UserAgent = context.Request.Headers["User-Agent"].ToString();
                    }

                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Session tracking failure should never block the actual request
                    System.Diagnostics.Debug.WriteLine($"[SessionTracking] Non-critical error: {ex.Message}");
                }
            }

            await _next(context);
        }
        
        private string GetDeviceName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";

            if (userAgent.Contains("iPhone")) return "iPhone";
            if (userAgent.Contains("Android")) return "Android Device";
            if (userAgent.Contains("iPad")) return "iPad";
            if (userAgent.Contains("Windows")) return "Windows PC";
            if (userAgent.Contains("Macintosh")) return "MacBook / iMac";
            if (userAgent.Contains("Linux")) return "Linux PC";

            return "Web Browser";
        }
    }

    public static class SessionTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionTrackingMiddleware>();
        }
    }
}

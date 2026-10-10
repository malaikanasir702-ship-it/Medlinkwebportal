using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MedLinkPortal.Services;

namespace MedLinkPortal.Middleware
{
    public class DataGovernanceMiddleware
    {
        private readonly RequestDelegate _next;

        public DataGovernanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Check if accessing sensitive patient or clinical records
            if (path.StartsWith("/api/patient", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/upload/clinical-document", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/doctor/classified-cases", StringComparison.OrdinalIgnoreCase))
            {
                var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var role = context.User?.FindFirstValue(ClaimTypes.Role) ?? "Anonymous";
                var ip = context.Connection.RemoteIpAddress?.ToString();
                var method = context.Request.Method;

                using var scope = serviceProvider.CreateScope();
                var governanceService = scope.ServiceProvider.GetService<IDataGovernanceService>();

                if (governanceService != null)
                {
                    try
                    {
                        var accessType = method == "GET" ? "Read" : "Write";
                        await governanceService.LogAccessAsync(
                            userId,
                            role,
                            null,
                            path,
                            null,
                            accessType,
                            ip,
                            $"API {method} access to {path}"
                        );
                    }
                    catch { }
                }
            }

            await _next(context);
        }
    }

    public static class DataGovernanceMiddlewareExtensions
    {
        public static IApplicationBuilder UseDataGovernance(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DataGovernanceMiddleware>();
        }
    }
}


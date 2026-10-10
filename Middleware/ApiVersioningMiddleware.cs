using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MedLinkPortal.Middleware
{
    public class ApiVersioningMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiVersioningMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Read version from header 'X-Api-Version' or query param 'api-version', default to '1.0'
            string version = "1.0";

            if (context.Request.Headers.TryGetValue("X-Api-Version", out var headerVersion) && !string.IsNullOrEmpty(headerVersion))
            {
                version = headerVersion.ToString();
            }
            else if (context.Request.Query.TryGetValue("api-version", out var queryVersion) && !string.IsNullOrEmpty(queryVersion))
            {
                version = queryVersion.ToString();
            }

            // Expose the negotiated version in items and response headers
            context.Items["ApiVersion"] = version;
            context.Response.Headers["X-Api-Version"] = version;

            await _next(context);
        }
    }
}

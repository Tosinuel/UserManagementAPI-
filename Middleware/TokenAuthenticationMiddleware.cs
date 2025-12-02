using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace UserManagementAPI.Middleware
{
    public class TokenAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenAuthenticationMiddleware> _logger;

        // For demo purposes; in real apps use proper token validation
        private const string DemoValidToken = "secrettoken123";

        public TokenAuthenticationMiddleware(RequestDelegate next, ILogger<TokenAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Allow anonymous access to swagger and health checks
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/swagger") || path.StartsWith("/index.html"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("Authorization", out var auth))
            {
                await ReturnUnauthorized(context, "Missing Authorization header");
                return;
            }

            var token = auth.ToString().Replace("Bearer ", "");
            if (string.IsNullOrWhiteSpace(token) || token != DemoValidToken)
            {
                await ReturnUnauthorized(context, "Invalid token");
                return;
            }

            await _next(context);
        }

        private static Task ReturnUnauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new { error = "Unauthorized", details = message });
            return context.Response.WriteAsync(payload);
        }
    }
}

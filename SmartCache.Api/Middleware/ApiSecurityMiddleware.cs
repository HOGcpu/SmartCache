using System.Net;

namespace SmartCache.Api.Middleware
{
    public class ApiSecurityMiddleware
    {
        // Proof-of-concept middleware
        // NOTE: This does not call the next middleware in the pipeline.
        // In a real production setup, _next(context) must be called
        // to allow the request to reach controllers and other middleware.

        private readonly string _apiKey;
        private readonly string? _bearerToken;

        public ApiSecurityMiddleware(RequestDelegate next, IConfiguration config)
        {
            //_next = next;
            _apiKey = config["Security:ApiKey"] ?? throw new ArgumentNullException("ApiKey missing in config");
            _bearerToken = config["Security:BearerToken"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check API Key
            if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader) || apiKeyHeader != _apiKey)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            // Check optional Bearer token
            if (!string.IsNullOrEmpty(_bearerToken))
            {
                if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
                    !authHeader.ToString().StartsWith("Bearer ") ||
                    authHeader.ToString().Substring("Bearer ".Length) != _bearerToken)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid Bearer token");
                    return;
                }
            }

            // All good, call next middleware (intentionally omitted for proof-of-concept)
            //await _next(context);
        }
    }
}

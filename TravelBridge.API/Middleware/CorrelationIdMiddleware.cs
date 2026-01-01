namespace TravelBridge.API.Middleware
{
    /// <summary>
    /// Middleware that handles session and request correlation IDs for tracing.
    /// - SessionId: Passed from frontend via X-Session-Id header, persists across user session
    /// - RequestId: Generated per request for individual request tracing
    /// Both are included in all log entries via Serilog enrichment.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string SessionIdHeader = "X-Session-Id";
        private const string RequestIdHeader = "X-Request-Id";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get or generate Session ID (from frontend or create new)
            var sessionId = context.Request.Headers[SessionIdHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N")[..12]; // Short ID for new sessions
            }

            // Always generate a new Request ID per request
            var requestId = context.Request.Headers[RequestIdHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = Guid.NewGuid().ToString("N")[..8]; // Short ID for requests
            }

            // Store in HttpContext for access throughout the request
            context.Items["SessionId"] = sessionId;
            context.Items["RequestId"] = requestId;

            // Add to response headers for frontend tracking
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[SessionIdHeader] = sessionId;
                context.Response.Headers[RequestIdHeader] = requestId;
                return Task.CompletedTask;
            });

            // Enrich Serilog log context with correlation IDs
            using (Serilog.Context.LogContext.PushProperty("SessionId", sessionId))
            using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
            {
                await _next(context);
            }
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}

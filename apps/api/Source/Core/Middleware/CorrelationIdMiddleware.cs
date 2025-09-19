using Serilog.Context;

namespace GameGuild.Core.Middleware;

/// <summary>
/// Middleware to add correlation ID to all requests and log context
/// </summary>
public class CorrelationIdMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        var correlationId = GetOrCreateCorrelationId(context);

        // Store in HttpContext.Items for other middleware to access
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Add to log context for the duration of the request
        using (LogContext.PushProperty("CorrelationId", correlationId)) {
            _logger.LogDebug("Processing request {RequestPath} with correlation ID {CorrelationId}",
                context.Request.Path, correlationId);

            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context) {
        // Check if correlation ID is already present in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrEmpty(correlationId)) {
            return correlationId.ToString();
        }

        // Generate a new correlation ID
        return Guid.NewGuid().ToString("D");
    }
}

/// <summary>
/// Extension methods for registering the correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions {
    /// <summary>
    /// Adds the correlation ID middleware to the application pipeline.
    /// This should be added early in the pipeline to ensure correlation IDs are available for all requests.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}

using System.Net;
using System.Text.RegularExpressions;
using GameGuild.Core.Services;
using Microsoft.Extensions.Options;

namespace GameGuild.Core.Middleware;

/// <summary>
/// Middleware for enforcing rate limits per endpoint, user, and IP address
/// Integrates with the IRateLimitingService for comprehensive rate limiting
/// </summary>
public partial class RateLimitingMiddleware {
    [GeneratedRegex(@"/\d+(?=/|$)", RegexOptions.IgnoreCase)]
    private static partial Regex NumberIdRegex();

    [GeneratedRegex(@"/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?=/|$)", RegexOptions.IgnoreCase)]
    private static partial Regex GuidIdRegex();
    private readonly RequestDelegate _next;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
      RequestDelegate next,
      IOptions<RateLimitingOptions> options,
      ILogger<RateLimitingMiddleware> logger) {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        // Skip rate limiting for certain paths or methods
        if (ShouldSkipRateLimit(context)) {
            await _next(context);
            return;
        }

        // Resolve the rate limiting service from the request scope
        var rateLimitingService = context.RequestServices.GetRequiredService<IRateLimitingService>();
        var endpoint = GetEndpointIdentifier(context);

        try {
            // Check rate limit before processing request
            var checkResult = await rateLimitingService.CheckRateLimitAsync(context, endpoint);

            if (!checkResult.IsAllowed) {
                await HandleRateLimitExceeded(context, checkResult);
                return;
            }

            // Process the request
            await _next(context);

            // Record successful request (only after successful processing)
            if (context.Response.StatusCode < 400) {
                await rateLimitingService.RecordRequestAsync(context, endpoint);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in rate limiting middleware for endpoint: {Endpoint}", endpoint);

            // Continue with request even if rate limiting fails
            await _next(context);
        }
    }

    private bool ShouldSkipRateLimit(HttpContext context) {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip for health checks and monitoring endpoints
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/ping", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        // Skip for OPTIONS requests (CORS preflight)
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        // Skip for WebSocket requests
        if (context.WebSockets.IsWebSocketRequest) {
            return true;
        }

        return false;
    }

    private string GetEndpointIdentifier(HttpContext context) {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Normalize common endpoint patterns
        var normalizedPath = NormalizePath(path);

        return $"{method}:{normalizedPath}";
    }

    private string NormalizePath(string path) {
        // Remove trailing slashes
        path = path.TrimEnd('/');

        // Replace common ID patterns with placeholders for better grouping
        path = NumberIdRegex().Replace(path, "/{id}");

        path = GuidIdRegex().Replace(path, "/{guid}");

        return path.ToLowerInvariant();
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitCheckResult checkResult) {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = "Various limits apply";
        context.Response.Headers["X-RateLimit-Remaining"] = "0";

        if (checkResult.RetryAfter.HasValue) {
            var retryAfterSeconds = (int)Math.Ceiling(checkResult.RetryAfter.Value.TotalSeconds);
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.Headers["X-RateLimit-Reset"] =
              DateTimeOffset.UtcNow.Add(checkResult.RetryAfter.Value).ToUnixTimeSeconds().ToString();
        }

        var response = new {
            error = "Rate limit exceeded",
            message = checkResult.Reason ?? "Too many requests",
            retryAfter = checkResult.RetryAfter?.TotalSeconds,
            timestamp = DateTimeOffset.UtcNow
        };

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);

        // Log the rate limit event for monitoring
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        _logger.LogWarning("Rate limit exceeded for User: {UserId}, IP: {IpAddress}, Endpoint: {Endpoint}, Reason: {Reason}",
          userId ?? "Anonymous",
          ipAddress ?? "Unknown",
          GetEndpointIdentifier(context),
          checkResult.Reason);
    }
}

/// <summary>
/// Extension methods for adding rate limiting middleware to the application pipeline
/// </summary>
public static class RateLimitingMiddlewareExtensions {
    /// <summary>
    /// Adds the rate limiting middleware to the application pipeline
    /// Should be called early in the pipeline, after authentication but before authorization
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder) {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Middlewares;

/// <summary>
/// Middleware that provides idempotency support for POST/PUT/PATCH requests.
/// Clients can include an Idempotency-Key header to prevent duplicate processing
/// of requests that may be retried due to network issues.
/// </summary>
public class IdempotencyMiddleware
{
    /// <summary>
    /// The header name for the idempotency key
    /// </summary>
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    
    /// <summary>
    /// The header indicating if the response was retrieved from cache
    /// </summary>
    public const string IdempotencyReplayedHeader = "Idempotency-Replayed";
    
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public IdempotencyMiddleware(
        RequestDelegate next, 
        ILogger<IdempotencyMiddleware> logger,
        IMemoryCache cache,
        IdempotencyOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _cacheDuration = options?.CacheDuration ?? TimeSpan.FromHours(24);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply idempotency to mutating requests
        var method = context.Request.Method;
        if (!IsMutatingMethod(method))
        {
            await _next(context);
            return;
        }

        // Check for idempotency key
        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKey) || 
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // No idempotency key provided - process normally
            await _next(context);
            return;
        }

        var cacheKey = BuildCacheKey(context, idempotencyKey!);
        
        // Check if we have a cached response
        if (_cache.TryGetValue(cacheKey, out IdempotentResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation(
                "Replaying idempotent response for key {IdempotencyKey}, Path: {Path}",
                idempotencyKey, context.Request.Path);
            
            await WriteCachedResponse(context, cachedResponse);
            return;
        }

        // Check if request is in-flight (to prevent race conditions)
        var inFlightKey = $"{cacheKey}:in-flight";
        if (_cache.TryGetValue(inFlightKey, out _))
        {
            _logger.LogWarning(
                "Request with idempotency key {IdempotencyKey} is already in progress",
                idempotencyKey);
            
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflict",
                Status = 409,
                Detail = "A request with this idempotency key is already being processed"
            });
            return;
        }

        // Mark request as in-flight
        _cache.Set(inFlightKey, true, TimeSpan.FromMinutes(5));

        try
        {
            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Cache successful responses (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(responseBody).ReadToEndAsync();
                
                var idempotentResponse = new IdempotentResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    body,
                    context.Response.Headers
                        .Where(h => !h.Key.StartsWith("Transfer-", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(h => h.Key, h => h.Value.ToString()));

                _cache.Set(cacheKey, idempotentResponse, _cacheDuration);
                
                _logger.LogInformation(
                    "Cached idempotent response for key {IdempotencyKey}, Status: {StatusCode}",
                    idempotencyKey, context.Response.StatusCode);
            }

            // Write response to client
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
        finally
        {
            // Remove in-flight marker
            _cache.Remove(inFlightKey);
        }
    }

    private static bool IsMutatingMethod(string method)
    {
        return method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
               method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
               method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCacheKey(HttpContext context, string idempotencyKey)
    {
        // Include user identity if authenticated for proper scoping
        var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
        var tenantId = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tid) ? tid.ToString() : "default";
        
        return $"idempotency:{tenantId}:{userId}:{context.Request.Path}:{idempotencyKey}";
    }

    private static async Task WriteCachedResponse(HttpContext context, IdempotentResponse cachedResponse)
    {
        context.Response.StatusCode = cachedResponse.StatusCode;
        context.Response.ContentType = cachedResponse.ContentType;
        context.Response.Headers.Append(IdempotencyReplayedHeader, "true");
        
        foreach (var header in cachedResponse.Headers)
        {
            if (!context.Response.Headers.ContainsKey(header.Key))
            {
                context.Response.Headers.Append(header.Key, header.Value);
            }
        }

        await context.Response.WriteAsync(cachedResponse.Body);
    }
}

/// <summary>
/// Cached response for idempotent requests
/// </summary>
public record IdempotentResponse(
    int StatusCode,
    string ContentType,
    string Body,
    Dictionary<string, string> Headers);

/// <summary>
/// Configuration options for idempotency middleware
/// </summary>
public class IdempotencyOptions
{
    /// <summary>
    /// How long to cache idempotent responses (default: 24 hours)
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Extension methods for registering the idempotency middleware
/// </summary>
public static class IdempotencyMiddlewareExtensions
{
    /// <summary>
    /// Adds idempotency support to the application pipeline.
    /// Place after authentication but before endpoint routing.
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app, Action<IdempotencyOptions>? configure = null)
    {
        var options = new IdempotencyOptions();
        configure?.Invoke(options);
        
        return app.UseMiddleware<IdempotencyMiddleware>(options);
    }
}

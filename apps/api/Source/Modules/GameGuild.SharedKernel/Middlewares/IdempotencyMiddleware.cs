using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GameGuild;

/// <summary>
/// Middleware that provides idempotency support for POST/PUT/PATCH requests.
/// Clients can include an Idempotency-Key header to prevent duplicate processing
/// of requests that may be retried due to network issues.
/// </summary>
/// <remarks>
/// <para>
/// <b>Multi-instance support (issue #1248 N2):</b> the default store is
/// <see cref="DistributedCacheIdempotencyStore"/>, backed by <c>IDistributedCache</c>
/// (Redis in production, the distributed memory cache in single-node/dev), so cached
/// idempotent responses are shared across every application instance.
/// </para>
/// <para>
/// <b>Concurrency caveat:</b> <c>IDistributedCache</c> has no atomic set-if-absent, so the
/// in-flight check cannot fully exclude two instances processing the same brand-new key
/// concurrently (that needs a distributed lock such as RedLock — see
/// <see cref="DistributedCacheIdempotencyStore"/>). Replay correctness is preserved.
/// </para>
/// </remarks>
public sealed class IdempotencyMiddleware
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
    private readonly IIdempotencyStore _store;
    private readonly TimeSpan _cacheDuration;

    public IdempotencyMiddleware(
        RequestDelegate next, 
        ILogger<IdempotencyMiddleware> logger,
        IIdempotencyStore store,
        IdempotencyOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _store = store;
        _cacheDuration = options?.CacheDuration ?? TimeSpan.FromHours(24);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply idempotency to mutating requests
        var method = context.Request.Method;
        if (!IsMutatingMethod(method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Check for idempotency key
        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKey) || 
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // No idempotency key provided - process normally
            await _next(context).ConfigureAwait(false);
            return;
        }

        var cacheKey = BuildCacheKey(context, idempotencyKey!);
        
        // Check if we have a cached response
        var cachedResponse = await _store.TryGetResponseAsync(cacheKey).ConfigureAwait(false);
        if (cachedResponse != null)
        {
            _logger.LogInformation(
                "Replaying idempotent response for key {IdempotencyKey}, Path: {Path}",
                LogRedaction.Sanitize(idempotencyKey), LogRedaction.Sanitize(context.Request.Path.Value));
            
            await WriteCachedResponse(context, cachedResponse).ConfigureAwait(false);
            return;
        }

        // Check if request is in-flight (to prevent race conditions)
        if (!await _store.TryMarkInFlightAsync(cacheKey, TimeSpan.FromMinutes(5)))
        {
            _logger.LogWarning(
                "Request with idempotency key {IdempotencyKey} is already in progress",
                idempotencyKey.ToString());
            
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = RfcUrls.Conflict,
                Title = "Conflict",
                Status = 409,
                Detail = "A request with this idempotency key is already being processed"
            });
            return;
        }

        try
        {
            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context).ConfigureAwait(false);

            // Cache successful responses (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(responseBody, leaveOpen: true);
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                
                var idempotentResponse = new IdempotentResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    body,
                    context.Response.Headers
                        .Where(h => !h.Key.StartsWith("Transfer-", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(h => h.Key, h => h.Value.ToString()));

                await _store.SetResponseAsync(cacheKey, idempotentResponse, _cacheDuration).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "Cached idempotent response for key {IdempotencyKey}, Status: {StatusCode}",
                    idempotencyKey, context.Response.StatusCode);
            }

            // Write response to client
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            context.Response.Body = originalBodyStream;
        }
        finally
        {
            // Remove in-flight marker
            await _store.RemoveInFlightAsync(cacheKey).ConfigureAwait(false);
        }
    }

    private static bool IsMutatingMethod(string method)
    {
        return method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
               method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
               method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase);
    }

    [ExcludeFromCodeCoverage]
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

        await context.Response.WriteAsync(cachedResponse.Body).ConfigureAwait(false);
    }
}

/// <summary>
/// Cached response for idempotent requests
/// </summary>
public sealed record IdempotentResponse(
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
    /// Requires <see cref="AddIdempotency"/> to have been called on the service collection,
    /// or an <see cref="IIdempotencyStore"/> to have been registered manually.
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app, Action<IdempotencyOptions>? configure = null)
    {
        var options = new IdempotencyOptions();
        configure?.Invoke(options);

        return app.UseMiddleware<IdempotencyMiddleware>(options);
    }

    /// <summary>
    /// Registers idempotency services in the DI container.
    /// Call this before <see cref="UseIdempotency"/>.
    /// The default store is <see cref="DistributedCacheIdempotencyStore"/> over
    /// <c>IDistributedCache</c> (issue #1248 N2 — multi-instance safe); a local
    /// distributed-memory provider is installed only when no other provider exists.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddIdempotency(this IServiceCollection services, Action<IdempotencyOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddDistributedIdempotencyStore();

        return services;
    }
}

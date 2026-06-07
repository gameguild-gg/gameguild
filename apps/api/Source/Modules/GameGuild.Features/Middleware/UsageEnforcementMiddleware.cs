using GameGuild.Identity.Context.Actors;
using GameGuild.Commerce.Subscriptions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Middleware for enforcing subscription usage limits (API calls, storage, users)
/// </summary>
public class UsageEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UsageEnforcementMiddleware> _logger;
    private readonly IMemoryCache _cache;

    // Cache keys
    private const string ApiCallsCacheKeyPrefix = "api_calls_";
    [ExcludeFromCodeCoverage]
    private static TimeSpan CacheExpiration { get; } = TimeSpan.FromMinutes(5);

    public UsageEnforcementMiddleware(
        RequestDelegate next,
        ILogger<UsageEnforcementMiddleware> logger,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IActorContextAccessor actorContextAccessor,
        ISubscriptionQueryService subscriptionQueryService,
        ISubscriptionPlanService subscriptionPlanService)
    {
        var actor = actorContextAccessor.ActorContext;
        
        // Skip enforcement for non-tenant requests
        if (actor.TenantId == null)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var tenantId = actor.TenantId.Value;
        var path = httpContext.Request.Path.ToString();

        // Skip enforcement for health checks and static files
        if (path.StartsWith("/health") || path.StartsWith("/api/health") || 
            path.StartsWith("/_") || path.Contains("."))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        try
        {
            // Get tenant's active subscription
            var subscription = await subscriptionQueryService.GetActiveTenantSubscriptionAsync(tenantId).ConfigureAwait(false);
            
            if (subscription == null)
            {
                _logger.LogWarning("No active subscription found for tenant {TenantId}", tenantId);
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            // Get subscription plan
            var subscriptionPlan = await subscriptionPlanService.GetByIdAsync(subscription.PlanId).ConfigureAwait(false);
            
            if (subscriptionPlan == null)
            {
                _logger.LogWarning("No subscription plan found for tenant {TenantId}", tenantId);
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            // Check API call limits
            if (subscriptionPlan.MaxApiCallsPerMonth.HasValue)
            {
                var cacheKey = $"{ApiCallsCacheKeyPrefix}{tenantId}_{SystemClock.UtcNow:yyyyMM}";
                var currentCalls = _cache.GetOrCreate(cacheKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
                    return 0L;
                });

                if (currentCalls >= subscriptionPlan.MaxApiCallsPerMonth.Value)
                {
                    _logger.LogWarning(
                        "API call limit exceeded for tenant {TenantId}. Current: {Current}, Limit: {Limit}",
                        tenantId,
                        currentCalls,
                        subscriptionPlan.MaxApiCallsPerMonth.Value);

                    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "API call limit exceeded",
                            message = $"You have exceeded your monthly API call limit of {subscriptionPlan.MaxApiCallsPerMonth.Value}",
                            limit = subscriptionPlan.MaxApiCallsPerMonth.Value,
                            current = currentCalls,
                            resetDate = new DateTime(SystemClock.UtcNow.Year, SystemClock.UtcNow.Month, 1).AddMonths(1)
                        }));
                    return;
                }

                // Increment counter
                _cache.Set(cacheKey, currentCalls + 1, CacheExpiration);
            }

            // Add usage headers for monitoring
            httpContext.Response.Headers.Append("X-RateLimit-Limit", 
                subscriptionPlan.MaxApiCallsPerMonth?.ToString() ?? "unlimited");
            httpContext.Response.Headers.Append("X-Subscription-Plan", 
                subscriptionPlan.Name);

            await _next(httpContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing usage limits for tenant {TenantId}", tenantId);
            // Continue execution even if usage enforcement fails
            await _next(httpContext).ConfigureAwait(false);
            throw;
        }
    }
}

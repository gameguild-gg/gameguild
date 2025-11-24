using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.SubscriptionPlans.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features.Middleware;

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
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

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
        ITenantContext tenantContext,
        ISubscriptionService subscriptionService,
        ISubscriptionPlanService subscriptionPlanService)
    {
        // Skip enforcement for non-tenant requests
        if (tenantContext.TenantId == null)
        {
            await _next(httpContext);
            return;
        }

        var tenantId = tenantContext.TenantId.Value;
        var path = httpContext.Request.Path.ToString();

        // Skip enforcement for health checks and static files
        if (path.StartsWith("/health") || path.StartsWith("/api/health") || 
            path.StartsWith("/_") || path.Contains("."))
        {
            await _next(httpContext);
            return;
        }

        try
        {
            // Get tenant's active subscription
            var subscription = await subscriptionService.GetActiveTenantSubscriptionAsync(tenantId);
            
            if (subscription == null)
            {
                _logger.LogWarning("No active subscription found for tenant {TenantId}", tenantId);
                await _next(httpContext);
                return;
            }

            // Get subscription plan
            var subscriptionPlan = await subscriptionPlanService.GetByIdAsync(subscription.PlanId);
            
            if (subscriptionPlan == null)
            {
                _logger.LogWarning("No subscription plan found for tenant {TenantId}", tenantId);
                await _next(httpContext);
                return;
            }

            // Check API call limits
            if (subscriptionPlan.MaxApiCallsPerMonth.HasValue)
            {
                var cacheKey = $"{ApiCallsCacheKeyPrefix}{tenantId}_{DateTime.UtcNow:yyyyMM}";
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
                            resetDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1)
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
                subscriptionPlan.Name ?? "Unknown");

            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing usage limits for tenant {TenantId}", tenantId);
            // Continue execution even if usage enforcement fails
            await _next(httpContext);
        }
    }
}

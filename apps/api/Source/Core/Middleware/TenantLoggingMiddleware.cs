using Serilog.Context;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;

namespace GameGuild.Core.Middleware;

/// <summary>
/// Middleware that adds tenant context to logging
/// </summary>
public class TenantLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get tenant ID from various sources
        var tenantId = GetTenantId(context);

        if (!string.IsNullOrEmpty(tenantId))
        {
            using (LogContext.PushProperty("TenantId", tenantId))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private static string? GetTenantId(HttpContext context)
    {
        // Try to get from header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            return tenantHeader.FirstOrDefault();
        }

        // Try to get from route
        if (context.GetRouteValue("tenantId") is string routeTenantId)
        {
            return routeTenantId;
        }

        // Try to get from query string
        if (context.Request.Query.TryGetValue("tenantId", out var queryTenantId))
        {
            return queryTenantId.FirstOrDefault();
        }

        // Try to get from claims (if authenticated)
        var tenantClaim = context.User?.FindFirst("tenant_id");
        if (tenantClaim != null)
        {
            return tenantClaim.Value;
        }

        return null;
    }
}

/// <summary>
/// Extension methods for TenantLoggingMiddleware
/// </summary>
public static class TenantLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantLoggingMiddleware>();
    }
}

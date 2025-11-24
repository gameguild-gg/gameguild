using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Infrastructure.Middleware;

/// <summary>
///     Middleware that logs request context information (user, tenant, permissions) for debugging and auditing
/// </summary>
public class RequestContextLoggingMiddleware(RequestDelegate next, ILogger<RequestContextLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext, IUserContext userContext, ITenantContext tenantContext)
    {
        var requestId = httpContext.TraceIdentifier;
        var path = httpContext.Request.Path;
        var method = httpContext.Request.Method;

        // Log request start with context
        logger.LogInformation(
            "Request {RequestId} started: {Method} {Path} | User: {UserId} ({UserEmail}) | Tenant: {TenantId} ({TenantName}) | Authenticated: {IsAuthenticated}",
            requestId,
            method,
            path,
            userContext.UserId?.ToString() ?? "Anonymous",
            userContext.Email ?? "N/A",
            tenantContext.TenantId?.ToString() ?? "None",
            tenantContext.TenantName ?? "N/A",
            userContext.IsAuthenticated
        );

        // Add structured logging properties
        using (logger.BeginScope(
                   new Dictionary<string, object>
                   {
                       ["RequestId"] = requestId,
                       ["UserId"] = userContext.UserId?.ToString() ?? "Anonymous",
                       ["UserEmail"] = userContext.Email ?? "N/A",
                       ["TenantId"] = tenantContext.TenantId?.ToString() ?? "None",
                       ["TenantName"] = tenantContext.TenantName ?? "N/A",
                       ["IsAuthenticated"] = userContext.IsAuthenticated,
                       ["Roles"] = string.Join(", ", userContext.Roles)
                   }
               ))
        {
            var startTime = DateTime.UtcNow;

            try
            {
                await next(httpContext).ConfigureAwait(false);

                var duration = DateTime.UtcNow - startTime;
                var statusCode = httpContext.Response.StatusCode;

                // Log request completion
                logger.LogInformation("Request {RequestId} completed: {StatusCode} in {Duration}ms", requestId, statusCode, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;

                // Log request failure
                logger.LogError(ex, "Request {RequestId} failed after {Duration}ms: {ErrorMessage}", requestId, duration.TotalMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

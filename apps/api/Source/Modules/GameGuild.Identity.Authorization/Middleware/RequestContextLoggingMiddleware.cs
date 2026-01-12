using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Middleware that logs request context information (user, tenant, permissions) for debugging and auditing
/// </summary>
public class RequestContextLoggingMiddleware(RequestDelegate next, ILogger<RequestContextLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext, IActorContextAccessor actorContextAccessor)
    {
        var actor = actorContextAccessor.ActorContext;
        var requestId = httpContext.TraceIdentifier;
        var path = httpContext.Request.Path;
        var method = httpContext.Request.Method;

        // Log request start with context
        logger.LogInformation(
            "Request {RequestId} started: {Method} {Path} | User: {UserId} ({UserEmail}) | Tenant: {TenantId} | Authenticated: {IsAuthenticated}",
            requestId,
            method,
            path,
            actor.SubjectId ?? "Anonymous",
            actor.GetAttribute("email") ?? "N/A",
            actor.TenantId?.ToString() ?? "None",
            actor.IsAuthenticated
        );

        // Add structured logging properties
        using (logger.BeginScope(
                   new Dictionary<string, object>
                   {
                       ["RequestId"] = requestId,
                       ["UserId"] = actor.SubjectId ?? "Anonymous",
                       ["UserEmail"] = actor.GetAttribute("email") ?? "N/A",
                       ["TenantId"] = actor.TenantId?.ToString() ?? "None",
                       ["IsAuthenticated"] = actor.IsAuthenticated,
                       ["Roles"] = string.Join(", ", actor.Roles)
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

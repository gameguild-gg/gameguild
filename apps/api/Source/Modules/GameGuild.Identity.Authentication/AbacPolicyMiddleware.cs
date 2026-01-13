using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Middleware for ABAC policy evaluation.
/// </summary>
/// <remarks>
///     ⚠️ DEPRECATED: This middleware has been moved to GameGuild.Identity.Authorization module.
///     Use <see cref="Authorization.AbacPolicyMiddleware"/> instead.
/// </remarks>
[Obsolete("Use GameGuild.Identity.Authorization.AbacPolicyMiddleware instead. ABAC is an authorization concern.")]
public class AbacPolicyMiddleware(RequestDelegate next, ILogger<AbacPolicyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogTrace("ABAC policy middleware processing request");
        
        // Add ABAC evaluation headers
        context.Response.Headers.Append("X-ABAC-Policies", "enabled");

        await next(context);
    }
}

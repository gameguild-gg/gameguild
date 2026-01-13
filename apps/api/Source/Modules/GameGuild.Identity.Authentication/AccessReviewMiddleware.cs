using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Middleware for access review compliance.
/// </summary>
/// <remarks>
///     ⚠️ DEPRECATED: This middleware has been moved to GameGuild.Identity.Authorization module.
///     Use <see cref="Authorization.AccessReviewMiddleware"/> instead.
/// </remarks>
[Obsolete("Use GameGuild.Identity.Authorization.AccessReviewMiddleware instead. Access reviews are an authorization concern.")]
public class AccessReviewMiddleware(RequestDelegate next, ILogger<AccessReviewMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogTrace("Access review middleware processing request");
        
        // Add access review headers
        context.Response.Headers.Append("X-Access-Review", "enabled");

        await next(context);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Middleware for access review compliance
/// </summary>
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

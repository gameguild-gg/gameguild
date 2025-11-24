using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Presentation;

/// <summary>
///     Middleware for access review compliance
/// </summary>
public class AccessReviewMiddleware(RequestDelegate next, ILogger<AccessReviewMiddleware> logger)
{
    private readonly ILogger<AccessReviewMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Add access review headers
        context.Response.Headers.Append("X-Access-Review", "enabled");

        await next(context);
    }
}

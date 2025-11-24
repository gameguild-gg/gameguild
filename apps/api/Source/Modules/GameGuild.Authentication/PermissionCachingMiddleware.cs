using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Presentation;

/// <summary>
///     Middleware for permission caching optimization
/// </summary>
public class PermissionCachingMiddleware(RequestDelegate next, ILogger<PermissionCachingMiddleware> logger)
{
    private readonly ILogger<PermissionCachingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Add permission caching headers
        context.Response.Headers.Append("X-Permission-Cache", "enabled");

        await next(context);
    }
}

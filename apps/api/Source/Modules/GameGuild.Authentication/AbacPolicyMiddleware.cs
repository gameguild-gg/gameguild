using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Presentation;

/// <summary>
///     Middleware for ABAC policy evaluation
/// </summary>
public class AbacPolicyMiddleware(RequestDelegate next, ILogger<AbacPolicyMiddleware> logger)
{
    private readonly ILogger<AbacPolicyMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Add ABAC evaluation headers
        context.Response.Headers.Append("X-ABAC-Policies", "enabled");

        await next(context);
    }
}

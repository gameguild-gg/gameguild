using GameGuild.API;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Core.Middleware;

/// <summary>
/// Applies configured rate-limit deny lists before any endpoint limiter runs.
/// </summary>
public sealed class RateLimitAccessControlMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, RateLimitAccessOptions options)
    {
        if (options.IsDenylisted(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await RateLimitProblemDetailsWriter.WriteAsync(
                context,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "This user or IP address is not allowed to access the API.",
                    Instance = context.Request.Path
                },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }
}

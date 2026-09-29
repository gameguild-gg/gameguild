using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API;

/// <summary>
/// Writes rate-limit failures through the configured Problem Details service with a safe JSON fallback.
/// </summary>
internal static class RateLimitProblemDetailsWriter
{
    public static async Task WriteAsync(
        HttpContext context,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken)
    {
        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        problemDetails.Type ??= "about:blank";
        problemDetails.Instance ??= context.Request.Path;
        problemDetails.Extensions.TryAdd("errorCode", statusCode switch
        {
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status429TooManyRequests => "rate_limit_exceeded",
            StatusCodes.Status503ServiceUnavailable => "rate_limit_store_unavailable",
            _ => $"http_{statusCode}"
        });
        problemDetails.Extensions.TryAdd("traceId", context.TraceIdentifier);
        problemDetails.Extensions.TryAdd("correlationId", context.TraceIdentifier);
        context.Response.StatusCode = statusCode;

        var problemDetailsService = context.RequestServices?.GetService<IProblemDetailsService>();
        if (problemDetailsService is not null && await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails
            }).ConfigureAwait(false))
        {
            return;
        }

        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problemDetails,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

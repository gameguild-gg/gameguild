namespace GameGuild.Core.Middleware;

/// <summary>
/// Extension methods for adding rate limiting middleware to the application pipeline
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Adds the rate limiting middleware to the application pipeline
    /// Should be called early in the pipeline, after authentication but before authorization
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder) { return builder.UseMiddleware<RateLimitingMiddleware>(); }
}

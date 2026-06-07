using Microsoft.AspNetCore.Builder;

namespace GameGuild.Features;

/// <summary>
///     Extension methods for usage enforcement middleware
/// </summary>
public static class UsageEnforcementMiddlewareExtensions
{
    /// <summary>
    ///     Adds the usage enforcement middleware to the pipeline.
    ///     This middleware checks and enforces subscription limits for API usage.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseUsageEnforcement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UsageEnforcementMiddleware>();
    }
}

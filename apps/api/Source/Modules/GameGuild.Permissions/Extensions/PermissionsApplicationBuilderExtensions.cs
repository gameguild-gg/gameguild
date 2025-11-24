using GameGuild.Permissions.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace GameGuild.Permissions.Infrastructure.Extensions;

/// <summary>
///     Extension methods for IApplicationBuilder to configure permissions middleware
/// </summary>
public static class PermissionsApplicationBuilderExtensions
{
    /// <summary>
    ///     Adds the context middleware that populates user, tenant, permissions, and localization contexts
    ///     Should be called early in the pipeline, after authentication
    /// </summary>
    public static IApplicationBuilder UseContextMiddleware(this IApplicationBuilder app) { return app.UseMiddleware<ContextMiddleware>(); }

    /// <summary>
    ///     Adds request context logging middleware for debugging and auditing
    /// </summary>
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app) { return app.UseMiddleware<RequestContextLoggingMiddleware>(); }

    /// <summary>
    ///     Adds all permissions-related middleware in the correct order
    ///     This is a convenience method that calls both UseContextMiddleware and UseRequestContextLogging
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="enableLogging">Whether to enable request context logging</param>
    public static IApplicationBuilder UsePermissionsInfrastructure(this IApplicationBuilder app, bool enableLogging = true)
    {
        app.UseContextMiddleware();

        if (enableLogging) { app.UseRequestContextLogging(); }

        return app;
    }
}

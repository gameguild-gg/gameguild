using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Authentication module configuration for comprehensive permission management system
///     Integrates Domain, Application, Infrastructure, and Presentation layers
/// </summary>
public static class AuthenticationModule
{
    /// <summary>
    ///     Register all authentication services including advanced authorization features
    /// </summary>
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Application layer services
        services.AddAuthenticationApplication();

        // Register Data layer services  
        services.AddAuthenticationData(configuration);

        // Register Presentation layer services
        services.AddAuthenticationPresentation(configuration);

        return services;
    }

    /// <summary>
    ///     Configure the authentication module in the application pipeline
    /// </summary>
    public static IApplicationBuilder UseAuthenticationModule(this IApplicationBuilder app)
    {
        // Configure authentication middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Configure permission caching middleware
        app.UseMiddleware<PermissionCachingMiddleware>();

        // Configure ABAC policy evaluation middleware
        app.UseMiddleware<AbacPolicyMiddleware>();

        // Configure access review middleware for compliance
        app.UseMiddleware<AccessReviewMiddleware>();

        return app;
    }
}

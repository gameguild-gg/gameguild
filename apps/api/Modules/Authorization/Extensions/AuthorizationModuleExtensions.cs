using GameGuild.Core.Modules;

namespace GameGuild.Modules.Authorization;

/// <summary>
/// Extension methods for the Authorization module providing the standardized pattern.
/// </summary>
public static class AuthorizationModuleExtensions
{
    /// <summary>
    /// Registers the Authorization module using the IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services, IConfiguration configuration) { return services.AddModule<AuthorizationModule>(configuration); }

    /// <summary>
    /// Maps Authorization module endpoints using the IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseAuthorizationModule(this WebApplication app) { return app.UseModule<AuthorizationModule>(); }
}

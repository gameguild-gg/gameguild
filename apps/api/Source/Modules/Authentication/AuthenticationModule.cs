using FluentValidation;
using GameGuild.Core.Modules;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication.Validators;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Authentication module implementing the standardized IModule interface.
/// Provides comprehensive authentication services following Clean Architecture.
/// </summary>
public class AuthenticationModule : ModuleBase {
    public override string ModuleName => "Authentication";
    public override string ModuleVersion => "2.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        base.ConfigureServices(services, configuration);

        // Delegate to the existing AuthModuleDependencyInjection for service registration
        // This maintains compatibility while providing the new IModule interface
        return AuthModuleDependencyInjection.AddAuthModule(services, configuration);
    }

    public override WebApplication MapEndpoints(WebApplication app) {
        base.MapEndpoints(app);

        // Configure authentication middleware pipeline
        app.UseAuthentication()
           .UseAuthorization()
           .UseMiddleware<JwtAuthenticationMiddleware>();

        return app;
    }
}

/// <summary>
/// Extension methods for the Authentication module providing the new standardized pattern.
/// </summary>
public static class AuthenticationModuleExtensions {
    /// <summary>
    /// Registers the Authentication module using the new IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration) {
        return services.AddModule<AuthenticationModule>(configuration);
    }

    /// <summary>
    /// Maps Authentication module endpoints using the new IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseAuthenticationModule(this WebApplication app) {
        return app.UseModule<AuthenticationModule>();
    }
}

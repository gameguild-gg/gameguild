using GameGuild.Core.Domain.Permissions;
using GameGuild.Core.Infrastructure.Permissions;
using GameGuild.Core.Modules;
using GameGuild.Services;

namespace GameGuild.Source.Modules.Authorization;

/// <summary>
/// Authorization module implementing the standardized IModule interface.
/// Provides comprehensive authorization services following Clean Architecture and DAC patterns.
/// </summary>
[StandardizedModule("Comprehensive authorization services following Clean Architecture and DAC patterns")]
[ModuleVersion("1.0.0")]
public class AuthorizationModule : ModuleBase {
    public override string ModuleName => "Authorization";
    public override string ModuleVersion => "1.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        base.ConfigureServices(services, configuration);

        // Register Authorization services
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IModulePermissionService, ModulePermissionService>();
        services.AddScoped<ISimplePermissionService, SimplePermissionService>();

        // Register missing DAC and Resource Permission services
        services.AddScoped<IDacPermissionResolver, DacPermissionResolver>();
        services.AddScoped<IResourcePermissionService, ResourcePermissionService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app) {
        base.MapEndpoints(app);

        // Authorization module middleware should be configured here
        // This can include DAC authorization middleware and context middleware

        return app;
    }
}

/// <summary>
/// Extension methods for the Authorization module providing the standardized pattern.
/// </summary>
public static class AuthorizationModuleExtensions {
    /// <summary>
    /// Registers the Authorization module using the IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services, IConfiguration configuration) {
        return services.AddModule<AuthorizationModule>(configuration);
    }

    /// <summary>
    /// Maps Authorization module endpoints using the IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseAuthorizationModule(this WebApplication app) {
        return app.UseModule<AuthorizationModule>();
    }
}

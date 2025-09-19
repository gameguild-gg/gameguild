using GameGuild.Core.Modules;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Tenants module implementing the standardized IModule interface.
/// Manages tenant entities, domain services, and context management.
/// </summary>
public class TenantsModuleV2 : ModuleBase {
    public override string ModuleName => "Tenants";
    public override string ModuleVersion => "2.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        base.ConfigureServices(services, configuration);

        // Register core tenant services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantDomainService, TenantDomainService>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<TenantSettingsService>();

        // TODO: Add CQRS handlers registration when available

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app) {
        base.MapEndpoints(app);

        // Tenants module doesn't have specific middleware or endpoint mapping currently
        // This can be extended when needed for tenant-specific routes or middleware

        return app;
    }
}

/// <summary>
/// Extension methods for the Tenants module providing the new standardized pattern.
/// </summary>
public static class TenantsModuleV2Extensions {
    /// <summary>
    /// Registers the Tenants module using the new IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTenantsModuleV2(this IServiceCollection services, IConfiguration configuration) {
        return services.AddModule<TenantsModuleV2>(configuration);
    }

    /// <summary>
    /// Maps Tenants module endpoints using the new IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseTenantsModuleV2(this WebApplication app) {
        return app.UseModule<TenantsModuleV2>();
    }
}

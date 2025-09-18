using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Modules.Tenants;

/// <summary> Extension methods for registering Tenants module services </summary>
public static class TenantsModule {
    /// <summary> Adds Tenants module services to the service collection </summary>
    /// <param name="services"> The service collection </param>
    /// <returns> The service collection for chaining </returns>
    public static IServiceCollection AddTenantsModule(this IServiceCollection services) {
        // Register core tenant services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantDomainService, TenantDomainService>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<TenantSettingsService>();

        return services;
    }
}
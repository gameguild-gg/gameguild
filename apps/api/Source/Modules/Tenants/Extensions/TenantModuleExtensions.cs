namespace GameGuild.Modules.Tenants;

/// <summary>
/// Dependency injection configuration for Tenant Module
/// Clean Architecture - Dependency Inversion Principle
/// </summary>
public static class TenantModuleExtensions
{
    /// <summary>
    /// Registers tenant services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTenantServices(this IServiceCollection services)
    {
        // Register tenant repository
        services.AddScoped<ITenantRepository, TenantRepository>();

        // Register tenant services
        services.AddSingleton<ITenantCacheService, TenantCacheService>();
        services.AddScoped<ITenantService, TenantService>();

        // Register tenant context as scoped (per request)
        services.AddScoped<ITenantContext, TenantContext>();

        return services;
    }

    /// <summary>
    /// Adds tenant middleware to the application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app) { return app.UseMiddleware<TenantMiddleware>(); }
}

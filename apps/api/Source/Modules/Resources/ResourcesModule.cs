using GameGuild.Modules.Resources.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Modules.Resources;

/// <summary>
/// Extension methods for registering Resources module services
/// </summary>
public static class ResourcesModule {
    /// <summary>
    /// Registers all Resources module services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddResourcesModule(this IServiceCollection services) {
        // Register core services
        services.AddScoped<IResourceQuotaService, ResourceQuotaService>();

        return services;
    }
}
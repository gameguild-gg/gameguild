using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
///     Module registration for course discovery, featured content, and curated collections.
/// </summary>
public static class DiscoveryModule
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services)
    {
        services.AddScoped<IDiscoveryService, DiscoveryService>();

        return services;
    }
}

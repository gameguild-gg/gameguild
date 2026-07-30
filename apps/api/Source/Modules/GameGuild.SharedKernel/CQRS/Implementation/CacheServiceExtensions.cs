using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Extension methods for adding cache services
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    ///     Adds memory cache service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMemoryCacheService(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }
}

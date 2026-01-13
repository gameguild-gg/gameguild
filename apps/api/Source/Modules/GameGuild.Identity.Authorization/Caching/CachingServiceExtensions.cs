using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>
///     Extension methods for registering authorization caching services.
/// </summary>
public static class CachingServiceExtensions
{
    /// <summary>
    ///     Adds authorization caching services with optional Redis distributed cache.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional cache options configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method configures a hybrid caching strategy:
    ///         <list type="bullet">
    ///             <item>L1 (IMemoryCache): Always enabled, fast per-instance cache</item>
    ///             <item>L2 (IDistributedCache): Optional Redis cache for multi-instance deployments</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         To enable Redis:
    ///         <code>
    ///         services.AddAuthorizationCaching(options => options.UseDistributedCache = true);
    ///         services.AddStackExchangeRedisCache(options => 
    ///             options.Configuration = "localhost:6379");
    ///         </code>
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddAuthorizationCaching(
        this IServiceCollection services,
        Action<AuthorizationCacheOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Cache metrics (singleton for aggregated stats)
        services.AddSingleton<ICacheMetricsService, CacheMetricsService>();

        // Hybrid cache (scoped to allow tenant-specific behavior)
        services.AddScoped<IHybridPermissionCache>(sp =>
        {
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            var options = sp.GetRequiredService<IOptions<AuthorizationCacheOptions>>();
            var metrics = sp.GetRequiredService<ICacheMetricsService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HybridPermissionCache>>();

            // Only inject distributed cache if configured
            IDistributedCache? distributedCache = null;
            if (options.Value.UseDistributedCache)
            {
                distributedCache = sp.GetService<IDistributedCache>();
            }

            return new HybridPermissionCache(memoryCache, options, metrics, logger, distributedCache);
        });

        // Cache invalidation service (scoped)
        services.AddScoped<ICacheInvalidationService>(sp =>
        {
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            var versionStore = sp.GetRequiredService<ITenantSecurityVersionStore>();
            var hybridCache = sp.GetRequiredService<IHybridPermissionCache>();
            var metrics = sp.GetRequiredService<ICacheMetricsService>();
            var options = sp.GetRequiredService<IOptions<AuthorizationCacheOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CacheInvalidationService>>();

            return new CacheInvalidationService(memoryCache, versionStore, hybridCache, metrics, options, logger);
        });

        return services;
    }

    /// <summary>
    ///     Adds Redis distributed cache for authorization (optional).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="redisConnectionString">Redis connection string.</param>
    /// <param name="instanceName">Redis instance name prefix.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     Call this method BEFORE <see cref="AddAuthorizationCaching"/> if you want to use Redis.
    /// </remarks>
    public static IServiceCollection AddAuthorizationRedisCache(
        this IServiceCollection services,
        string redisConnectionString,
        string instanceName = "gg:auth:")
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = instanceName;
        });

        services.Configure<AuthorizationCacheOptions>(options =>
        {
            options.UseDistributedCache = true;
            options.RedisConnectionString = redisConnectionString;
            options.RedisInstanceName = instanceName;
        });

        return services;
    }
}

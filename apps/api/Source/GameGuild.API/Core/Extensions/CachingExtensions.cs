using GameGuild.Configuration;
using GameGuild.Configuration.InfrastructureLayer.MemoryCaching;
using GameGuild.Configuration.InfrastructureLayer.RedisCaching;
using GameGuild.Configuration.PresentationLayer.ResponseCaching;
using GameGuild.CQRS;
using GameGuild.CQRS.Implementation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring caching services.
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    ///     Sets up memory caching with configurable options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="options">Memory caching options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection SetupMemoryCaching(this IServiceCollection services, IConfiguration configuration,
        MemoryCachingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "MemoryCaching",
            MemoryCachingOptions.CreateDefault);
        options.Validate();

        services.AddMemoryCache(cacheOptions =>
            {
                cacheOptions.SizeLimit = options.SizeLimit;
                cacheOptions.CompactionPercentage = options.CompactionPercentage;
                cacheOptions.ExpirationScanFrequency = options.ExpirationScanFrequency;
            }
        );

        var redisOptions = OptionBuilderUtilities.CreateAndBind(
            configuration,
            RedisCachingOptions.SectionName,
            RedisCachingOptions.CreateDefault);

        if (redisOptions.Enabled)
        {
            redisOptions.Validate();

            var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString!);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectTimeout = redisOptions.ConnectTimeoutMs;
            configurationOptions.SyncTimeout = redisOptions.SyncTimeoutMs;

            services.AddStackExchangeRedisCache(cacheOptions =>
            {
                cacheOptions.ConfigurationOptions = configurationOptions;
                cacheOptions.InstanceName = redisOptions.InstanceName;
            });

            services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configurationOptions));
            services.AddSingleton<ICacheService, DistributedCacheService>();
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    ///     Sets up response caching with configurable options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="options">Response caching options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection SetupResponseCaching(this IServiceCollection services,
        IConfiguration configuration, ResponseCachingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ResponseCaching",
            ResponseCachingOptions.CreateDefault);
        options.Validate();

        services.AddResponseCaching(cachingOptions =>
            {
                cachingOptions.MaximumBodySize = options.MaximumBodySize;
                cachingOptions.UseCaseSensitivePaths = options.UseCaseSensitivePaths;
            }
        );

        return services;
    }
}

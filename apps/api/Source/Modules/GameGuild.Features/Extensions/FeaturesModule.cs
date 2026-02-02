


using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenFeature;

namespace GameGuild.Features;

/// <summary> Extension methods for registering Features module services </summary>
public static class FeaturesModule
{
    /// <summary> Registers all Features module services </summary>
    public static IServiceCollection AddFeaturesModule(this IServiceCollection services)
    {
        // Register repositories (ISP-compliant)
        services.AddScoped<IFeatureFlagQueryRepository, FeatureFlagQueryRepository>();
        services.AddScoped<IFeatureFlagTargetingRepository, FeatureFlagTargetingRepository>();
        services.AddScoped<IFeatureFlagAnalyticsRepository, FeatureFlagAnalyticsRepository>();

        // Register strategy pattern implementations
        services.AddScoped<IFeatureEvaluationStrategy, SimpleToggleStrategy>();
        services.AddScoped<IFeatureEvaluationStrategy, PercentageRolloutStrategy>();
        services.AddScoped<IFeatureEvaluationStrategy, TargetedEvaluationStrategy>();

        // Register chain of responsibility handlers (order doesn't matter - Priority property controls execution)
        services.AddScoped<ITargetingRuleHandler, TenantTargetingHandler>();
        services.AddScoped<ITargetingRuleHandler, UserTargetingHandler>();
        services.AddScoped<ITargetingRuleHandler, PlanTargetingHandler>();
        services.AddScoped<ITargetingRuleHandler, CountryTargetingHandler>();
        services.AddScoped<ITargetingRuleHandler, CustomTargetingHandler>();

        // Register core evaluation service with decorator pattern (factory for decorator chain)
        services.AddScoped<IFeatureFlagEvaluationService>(sp =>
            {
                // Create base service (innermost - core logic)
                var baseService = new FeatureFlagEvaluationService(
                    sp.GetRequiredService<IFeatureFlagQueryRepository>(),
                    sp.GetRequiredService<IEnumerable<IFeatureEvaluationStrategy>>(),
                    sp.GetRequiredService<ILogger<FeatureFlagEvaluationService>>(),
                    sp.GetRequiredService<IOptions<FeatureFlagOptions>>()
                );

                // Apply caching decorator
                var cachedService = new CachedFeatureFlagService(baseService, sp.GetRequiredService<IDistributedCache>());

                // Apply analytics decorator
                var analyticsService = new AnalyticsFeatureFlagService(cachedService, sp.GetRequiredService<IFeatureFlagAnalyticsService>());

                // Apply logging decorator (outermost)
                var loggedService = new LoggingFeatureFlagService(analyticsService, sp.GetRequiredService<ILogger<LoggingFeatureFlagService>>());

                return loggedService;
            }
        );

        // Register other segregated feature flag services (ISP-compliant)
        services.AddScoped<IFeatureFlagConfigurationService, FeatureFlagConfigurationService>();
        services.AddScoped<IFeatureFlagAnalyticsService, FeatureFlagAnalyticsService>();

        // Register subscription-feature integration service
        services.AddScoped<ISubscriptionFeatureService, SubscriptionFeatureService>();

        // Register capability service for tenant entitlements
        services.AddScoped<ICapabilityService, CapabilityService>();

        // Management and targeting services
        services.AddScoped<IFeatureFlagManagementService, FeatureFlagManagementService>();
        // services.AddScoped<IFeatureFlagTargetingService, FeatureFlagTargetingService>();

        // Register OpenFeature provider (singleton to maintain consistent state)
        services.AddSingleton<DatabaseFeatureFlagProvider>();
        
        // Register OpenFeature API
        services.AddSingleton(sp =>
        {
            var api = OpenFeature.Api.Instance;
            var provider = sp.GetRequiredService<DatabaseFeatureFlagProvider>();
            api.SetProviderAsync(provider).Wait();
            return api;
        });
        
        // Register OpenFeature hosted service for initialization
        services.AddHostedService<OpenFeatureHostedInitializer>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }
}

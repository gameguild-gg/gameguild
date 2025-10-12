using GameGuild.Modules.Features.Abstractions;
using GameGuild.Modules.Features.Services;

namespace GameGuild.Modules.Features;

/// <summary> Extension methods for registering Features module services </summary>
public static class FeaturesModule
{
    /// <summary> Registers all Features module services </summary>
    public static IServiceCollection AddFeaturesModule(this IServiceCollection services)
    {
        // Register feature flag service with tenant support
        services.AddScoped<IFeatureFlagService, TenantAwareFeatureFlagService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }
}
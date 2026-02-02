using GameGuild.Gamification.Achievements.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Gamification.Achievements;

/// <summary>
/// Module registration for the Achievements/Gamification system.
/// </summary>
public static class AchievementsModule
{
    /// <summary>
    /// Adds achievement services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAchievementsModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IAchievementService, AchievementService>();

        return services;
    }

    /// <summary>
    /// Maps achievement endpoints if using minimal APIs.
    /// </summary>
    public static IEndpointRouteBuilder MapAchievementsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Controllers are auto-discovered, but this can be used for minimal API routes
        return endpoints;
    }
}

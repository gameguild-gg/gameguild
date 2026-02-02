using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Ratings;

/// <summary>
/// Module registration for the Ratings feature
/// </summary>
public static class RatingsModule
{
    /// <summary>
    /// Registers all Ratings module services with the DI container
    /// </summary>
    public static IServiceCollection AddRatingsModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IRatingService, RatingService>();

        return services;
    }
}

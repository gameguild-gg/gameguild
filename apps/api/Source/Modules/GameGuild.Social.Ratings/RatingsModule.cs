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
        // Register focused sub-services
        services.AddScoped<IRatingCrudService, RatingCrudService>();
        services.AddScoped<IRatingQueryService, RatingQueryService>();
        services.AddScoped<IRatingModerationService, RatingModerationService>();

        // Register facade for backward compatibility (controllers, GraphQL resolvers)
        services.AddScoped<IRatingService, RatingService>();

        return services;
    }
}

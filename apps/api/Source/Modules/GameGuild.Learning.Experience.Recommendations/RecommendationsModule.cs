using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
///     Module registration for personalized course recommendations and learning profiles.
/// </summary>
public static class RecommendationsModule
{
    public static IServiceCollection AddRecommendationsModule(this IServiceCollection services)
    {
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IRecommendationEngine, RecommendationEngine>();
        services.AddScoped<IRecommendationStrategy, NextInPathStrategy>();
        services.AddScoped<IRecommendationStrategy, SimilarToCompletedStrategy>();
        services.AddScoped<IRecommendationStrategy, PopularInCategoryStrategy>();
        services.AddScoped<IRecommendationStrategy, TrendingNowStrategy>();

        return services;
    }
}

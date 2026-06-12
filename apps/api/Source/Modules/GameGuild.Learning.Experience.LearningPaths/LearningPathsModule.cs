using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
///     Module registration for curated learning paths and path progress tracking.
/// </summary>
public static class LearningPathsModule
{
    public static IServiceCollection AddLearningPathsModule(this IServiceCollection services)
    {
        services.AddScoped<ILearningPathService, LearningPathService>();

        return services;
    }
}

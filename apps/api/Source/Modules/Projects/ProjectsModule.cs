using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Extension methods for registering Projects module services
/// </summary>
public static class ProjectsModule {
    /// <summary>
    /// Add Projects module services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProjectsModule(this IServiceCollection services) {
        // Register Projects services
        services.AddScoped<IProjectService, ProjectService>();

        return services;
    }
}
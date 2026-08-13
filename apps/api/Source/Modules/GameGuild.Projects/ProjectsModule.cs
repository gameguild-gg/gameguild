using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Projects;

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
        // Register focused sub-services
        services.AddScoped<IProjectCrudService, ProjectCrudService>();
        services.AddScoped<IProjectEngagementService, ProjectEngagementService>();
        services.AddScoped<IProjectChannelAvailabilityService, ProjectChannelAvailabilityService>();
        services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
        services.AddScoped<IPermissionResolver, ProjectPermissionResolver>();
        services.AddScoped<IResourcePermissionService, ProjectResourcePermissionService>();
        services.AddScoped<IProjectLifecycleLock, ProjectLifecycleLock>();
        services.AddScoped<IProjectLifecycleParticipant, ProjectStoreProductLifecycleParticipant>();
        services.AddScoped<IProjectLifecycleCoordinator, ProjectLifecycleCoordinator>();

        // Register facade for backward compatibility
        services.AddScoped<IProjectService, ProjectService>();

        return services;
    }
}

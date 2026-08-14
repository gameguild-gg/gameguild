using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.ProjectWork;

public static class ProjectWorkModule
{
    public static IServiceCollection AddProjectWorkModule(this IServiceCollection services)
    {
        services.AddScoped<IProjectWorkService, ProjectWorkService>();
        return services;
    }
}

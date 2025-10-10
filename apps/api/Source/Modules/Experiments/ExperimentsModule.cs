using GameGuild.Modules.Experiments.Repositories;
using GameGuild.Modules.Experiments.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Modules.Experiments;

public static class ExperimentsModule
{
    public static IServiceCollection AddExperimentsModule(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IExperimentService, ExperimentService>();

        // Repositories
        services.AddScoped<IExperimentRepository, ExperimentRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();

        return services;
    }
}

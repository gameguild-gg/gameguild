using GameGuild.Modules.DataArchival.Repositories;
using GameGuild.Modules.DataArchival.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Modules.DataArchival;

/// <summary>
/// Module registration for Data Archival.
/// </summary>
public static class DataArchivalModule
{
    public static IServiceCollection AddDataArchivalModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IDataArchivalService, DataArchivalService>();
        services.AddScoped<IStorageLifecycleManager, StorageLifecycleManager>();

        // Register repositories
        services.AddScoped<IArchivalPolicyRepository, ArchivalPolicyRepository>();
        services.AddScoped<IArchivalJobRepository, ArchivalJobRepository>();

        return services;
    }
}

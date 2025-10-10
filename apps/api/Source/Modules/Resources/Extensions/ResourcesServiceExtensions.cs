using GameGuild.CQRS;
using GameGuild.Modules.Resources.Commands;
using GameGuild.Modules.Resources.Entities;
using GameGuild.Modules.Resources.Handlers;
using GameGuild.Modules.Resources.Infrastructure;
using GameGuild.Modules.Resources.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Modules.Resources.Extensions;

/// <summary>
/// Extension methods for configuring Resources module services
/// </summary>
public static class ResourcesServiceExtensions
{
    /// <summary>
    /// Add Resources module services to the DI container
    /// </summary>
    public static IServiceCollection AddResourcesModule(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IResourceUsageRepository, ResourceUsageRepository>();
        services.AddScoped<IResourceQuotaRepository, ResourceQuotaRepository>();

        // Register CQRS handlers
        services.AddScoped<IRequestHandler<GetUsageRecordsQuery, Result<IEnumerable<ResourceUsageRecord>>>, GetUsageRecordsHandler>();
        services.AddScoped<IRequestHandler<GetCurrentUsageSummaryQuery, Result<Dictionary<ResourceUsageType, long>>>, GetCurrentUsageSummaryHandler>();
        services.AddScoped<IRequestHandler<CheckUsageLimitsQuery, Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>>, CheckUsageLimitsHandler>();
        services.AddScoped<IRequestHandler<RecordUsageCommand, Result<ResourceUsageRecord>>, RecordUsageHandler>();

        return services;
    }
}
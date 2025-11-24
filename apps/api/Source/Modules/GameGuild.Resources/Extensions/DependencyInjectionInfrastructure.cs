using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Commands;
using GameGuild.Resources.Configuration;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;
using GameGuild.Resources.Queries;
using GameGuild.Resources.Repositories;
using GameGuild.Resources.Services;
using GameGuild.SharedKernel.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Resources.Extensions;

/// <summary>
///     Dependency injection configuration for Resources Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Registers all infrastructure services including DbContext and repositories
    /// </summary>
    public static IServiceCollection AddResourcesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Resources options using SharedKernel configuration utilities
        services.ConfigureOptions<ResourcesOptions>(configuration, () => new ResourcesOptions(), options => options.Validate());

        // Register DbContext
        RegisterDbContext(services, configuration);

        // Register Repositories
        RegisterRepositories(services);

        // Register Application Services
        RegisterApplicationServices(services, configuration);

        return services;
    }

    /// <summary>
    ///     Registers all Resources application services including command/query handlers
    /// </summary>
    private static void RegisterApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register Quota Management Commands
        services.AddScoped<ICommandHandler<SetResourceQuotaCommand>, SetResourceQuotaCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteResourceQuotaCommand>, DeleteResourceQuotaCommandHandler>();
        services.AddScoped<ICommandHandler<ResetResourceQuotaCommand>, ResetResourceQuotaCommandHandler>();
        services.AddScoped<ICommandHandler<ToggleResourceQuotaCommand>, ToggleResourceQuotaCommandHandler>();

        // Register Usage Recording Commands
        services.AddScoped<ICommandHandler<RecordResourceUsageCommand, Guid>, RecordResourceUsageCommandHandler>();
        services.AddScoped<ICommandHandler<ResetResourceUsageCommand>, ResetResourceUsageCommandHandler>();

        // Register Quota Queries
        services.AddScoped<IQueryHandler<GetResourceQuotaQuery, ResourceQuotaResponse?>, GetResourceQuotaQueryHandler>();
        services.AddScoped<IQueryHandler<GetTenantResourceQuotasQuery, IEnumerable<ResourceQuotaResponse>>, GetTenantResourceQuotasQueryHandler>();
        services.AddScoped<IQueryHandler<CheckResourceQuotaQuery, ResourceQuotaEnforcementResult>, CheckResourceQuotaQueryHandler>();

        // Register Usage Queries
        services.AddScoped<IQueryHandler<GetResourceUsageRecordsQuery, IEnumerable<UsageRecord>>, GetResourceUsageRecordsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCurrentResourceUsageSummaryQuery, Dictionary<ResourceUsageType, int>>, GetCurrentResourceUsageSummaryQueryHandler>();
        services.AddScoped<IQueryHandler<GetResourceUsageByTypeQuery, Dictionary<Guid, int>>, GetResourceUsageByTypeQueryHandler>();

        // Register Limit Checking Queries
        services.AddScoped<IQueryHandler<CheckResourceUsageLimitsQuery, Dictionary<ResourceUsageType, bool>>, CheckResourceUsageLimitsQueryHandler>();

        // Register Application Services
        services.AddScoped<IResourceQuotaService, ResourceQuotaService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IResourceThrottlingService, ResourceThrottlingService>();
        services.AddScoped<IUsageRetentionService, UsageRetentionService>();
        services.AddScoped<IUsageTrendAnalysisService, UsageTrendAnalysisService>();
        services.AddScoped<ISlaImpactAnalysisService, SlaImpactAnalysisService>();
        services.AddScoped<ICostAllocationService, CostAllocationService>();
    }

    /// <summary>
    ///     Register database-related services (the ApplicationDbContext is registered by the main API)
    ///     This method is kept for compatibility but no longer registers its own DbContext
    /// </summary>
    private static void RegisterDbContext(IServiceCollection services, IConfiguration configuration)
    {
        // NOTE: The Resources module now uses the shared ApplicationDbContext from GameGuild.API
        // This context is registered in the main API's DependencyInjection.AddInfrastructureData method
        // No need to register a separate ResourcesDbContext

        // The IApplicationDbContext service is already registered by the main API
        // and includes all Resources entities (ResourceQuota, UsageRecord, etc.)
    }

    /// <summary>
    ///     Register all repository implementations
    /// </summary>
    private static void RegisterRepositories(IServiceCollection services)
    {
        // Core Resources Repositories
        services.AddScoped<IResourceQuotaRepository, ResourceQuotaRepository>();
        services.AddScoped<IUsageRecordRepository, UsageRecordRepository>();
        services.AddScoped<ICostAllocationReportRepository, CostAllocationReportRepository>();
        services.AddScoped<IResourceThrottlingPolicyRepository, ResourceThrottlingPolicyRepository>();
        services.AddScoped<ISlaImpactAnalysisRepository, SlaImpactAnalysisRepository>();
        services.AddScoped<IUsageRetentionPolicyRepository, UsageRetentionPolicyRepository>();
        services.AddScoped<IResourceUsageTrendRepository, ResourceUsageTrendRepository>();
    }
}

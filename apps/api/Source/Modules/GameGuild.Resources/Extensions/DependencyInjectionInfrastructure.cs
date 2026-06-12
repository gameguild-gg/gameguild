using GameGuild.CQRS;
using GameGuild.Configuration;
using GameGuild.Resources.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GameGuild.Resources;

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
        services.ConfigureOptions(configuration, () => new ResourcesOptions(), options => options.Validate());

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

        // Register User-Level Quota Management Commands
        services.AddScoped<ICommandHandler<SetUserResourceQuotaCommand>, SetUserResourceQuotaCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteUserResourceQuotaCommand>, DeleteUserResourceQuotaCommandHandler>();
        services.AddScoped<ICommandHandler<ResetUserResourceQuotaCommand>, ResetUserResourceQuotaCommandHandler>();
        services.AddScoped<ICommandHandler<ToggleUserResourceQuotaCommand>, ToggleUserResourceQuotaCommandHandler>();

        // Register User-Level Usage Recording Commands
        services.AddScoped<ICommandHandler<RecordUserResourceUsageCommand, Guid>, RecordUserResourceUsageCommandHandler>();
        services.AddScoped<ICommandHandler<ResetUserResourceUsageCommand>, ResetUserResourceUsageCommandHandler>();

        // Register Quota Queries
        services.AddScoped<IQueryHandler<GetResourceQuotaQuery, ResourceQuotaResponse?>, GetResourceQuotaQueryHandler>();
        services.AddScoped<IQueryHandler<GetTenantResourceQuotasQuery, IEnumerable<ResourceQuotaResponse>>, GetTenantResourceQuotasQueryHandler>();
        services.AddScoped<IQueryHandler<CheckResourceQuotaQuery, ResourceQuotaEnforcementResult>, CheckResourceQuotaQueryHandler>();

        // Register Usage Queries
        services.AddScoped<IQueryHandler<GetResourceUsageRecordsQuery, PagedResult<UsageRecord>>, GetResourceUsageRecordsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCurrentResourceUsageSummaryQuery, Dictionary<ResourceUsageType, int>>, GetCurrentResourceUsageSummaryQueryHandler>();
        services.AddScoped<IQueryHandler<GetResourceUsageByTypeQuery, Dictionary<Guid, int>>, GetResourceUsageByTypeQueryHandler>();

        // Register Limit Checking Queries
        services.AddScoped<IQueryHandler<CheckResourceUsageLimitsQuery, Dictionary<ResourceUsageType, bool>>, CheckResourceUsageLimitsQueryHandler>();

        // Register User-Level Quota Queries
        services.AddScoped<IQueryHandler<GetUserResourceQuotaQuery, ResourceQuotaResponse?>, GetUserResourceQuotaQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserResourceQuotasQuery, IEnumerable<ResourceQuotaResponse>>, GetUserResourceQuotasQueryHandler>();
        services.AddScoped<IQueryHandler<CheckUserResourceQuotaQuery, ResourceQuotaEnforcementResult>, CheckUserResourceQuotaQueryHandler>();

        // Register User-Level Usage Queries
        services.AddScoped<IQueryHandler<GetUserResourceUsageRecordsQuery, IEnumerable<UsageRecord>>, GetUserResourceUsageRecordsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCurrentUserResourceUsageSummaryQuery, Dictionary<ResourceUsageType, long>>, GetCurrentUserResourceUsageSummaryQueryHandler>();

        // Register User-Level Limit Checking Queries
        services.AddScoped<IQueryHandler<CheckUserResourceUsageLimitsQuery, Dictionary<ResourceUsageType, bool>>, CheckUserResourceUsageLimitsQueryHandler>();

        // Register focused sub-services
        services.AddScoped<IQuotaManagementService, QuotaManagementService>();
        services.AddScoped<IQuotaEnforcementService, QuotaEnforcementService>();
        services.AddScoped<IQuotaMaintenanceService, QuotaMaintenanceService>();

        // Register the thin facade (used by CachedResourceQuotaService decorator)
        services.AddScoped<ResourceQuotaService>();

        // Register the unified IResourceQuotaService with caching decorator
        services.AddScoped<IResourceQuotaService>(sp =>
            new CachedResourceQuotaService(
                sp.GetRequiredService<ResourceQuotaService>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedResourceQuotaService>>()));

        // ISP: Register segregated interfaces pointing to the unified service
        services.AddScoped<IResourceQuotaReader>(sp => sp.GetRequiredService<IResourceQuotaService>());
        services.AddScoped<IResourceQuotaWriter>(sp => sp.GetRequiredService<IResourceQuotaService>());
        services.AddScoped<IResourceQuotaEnforcer>(sp => sp.GetRequiredService<IResourceQuotaService>());
        services.AddScoped<IResourceQuotaAnalytics>(sp => sp.GetRequiredService<IResourceQuotaService>());
        services.AddScoped<IResourceQuotaMaintenance>(sp => sp.GetRequiredService<IResourceQuotaService>());

        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IResourceThrottlingEnforcementSink, LocalResourceThrottlingEnforcementSink>();
        services.AddScoped<IResourceThrottlingService, ResourceThrottlingService>();
        if (configuration.GetValue<bool>("Redis:Enabled"))
        {
            services.AddScoped<IDistributedRateLimiter, RedisDistributedRateLimiter>();
        }
        else
        {
            services.AddScoped<IDistributedRateLimiter, DistributedCacheRateLimiter>();
        }

        services.AddScoped<IUsageRetentionService, UsageRetentionService>();
        services.AddScoped<IUsageRetentionArchiveSink, LocalUsageRetentionArchiveSink>();
        services.AddScoped<IUsagePatternRecognizer, HeuristicUsagePatternRecognizer>();
        services.AddScoped<IUsageTrendAnalysisService, UsageTrendAnalysisService>();
        services.AddScoped<ISlaImpactAnalysisService, SlaImpactAnalysisService>();
        services.AddScoped<ICostCenterValidator, ConfiguredCostCenterValidator>();
        services.AddScoped<ICostAllocationService, CostAllocationService>();

        // SLA Incident Escalation Services
        // SlaIncidentEscalationService depends on ISlaImpactAnalysisRepository + IIncidentTicketProvider
        // directly, avoiding the former circular dependency with ISlaImpactAnalysisService.
        services.AddScoped<ISlaNotificationSender, LoggingSlaNotificationSender>();
        services.AddScoped<ISlaIncidentEscalationService, SlaIncidentEscalationService>();
        services.AddScoped<IIncidentTicketProvider, DefaultIncidentTicketProvider>();

        // Event Handlers for Alerts and Observability
        services.AddScoped<INotificationHandler<QuotaExceededEvent>, QuotaExceededAlertHandler>();
    }

    /// <summary>
    ///     Register database-related services (the ApplicationDbContext is registered by the main API)
    ///     This method is kept for compatibility but no longer registers its own DbContext
    /// </summary>
    // ReSharper disable once UnusedParameter.Local - Parameters kept for signature compatibility
    private static void RegisterDbContext(IServiceCollection _, IConfiguration _2)
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

        // Metadata and Settings Repositories
        services.AddScoped<IResourceMetadataRepository, ResourceMetadataRepository>();
        services.AddScoped<IResourceSettingsRepository, ResourceSettingsRepository>();
    }
}

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Dependency injection extensions for the Tenants module.
///     NOTE: Repositories and services are now auto-discovered by the API layer based on naming conventions.
///     This class is kept for reference and backward compatibility but is no longer called.
/// </summary>
[Obsolete("Use automatic discovery in DependencyInjection.AddRepositories() instead. Services matching I*Service -> *Service and I*Repository -> *Repository are auto-discovered.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Tenants module services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddTenantsModule(this IServiceCollection services)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");

        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting Tenants module setup...");

        var stepStopwatch = Stopwatch.StartNew();

        // Register repositories
        stepStopwatch.Restart();
        services.AddScoped<ITenantRepository, TenantRepository>();
        logger.LogInformation("Registered Tenant Repository in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        stepStopwatch.Restart();
        services.AddScoped<ITenantDomainsRepository, TenantDomainsRepository>();
        logger.LogInformation("Registered Tenant Domains Repository in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        stepStopwatch.Restart();
        services.AddScoped<ITenantMemberRepository, TenantMemberRepository>();
        logger.LogInformation("Registered Tenant Member Repository in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        stepStopwatch.Restart();
        services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();
        logger.LogInformation("Registered Tenant Settings Repository in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        // Register services
        stepStopwatch.Restart();
        services.AddScoped<ITenantService, TenantService>();
        logger.LogInformation("Registered Tenant Service in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        stepStopwatch.Restart();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();
        logger.LogInformation("Registered Usage Tracking Service in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        totalStopwatch.Stop();
        logger.LogInformation("Completed Tenants module setup in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return services;
    }
}

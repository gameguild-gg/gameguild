using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Extension methods for registering Audit module services
/// </summary>
public static class AuditModule
{
    /// <summary>
    /// Registers all Audit module services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureSigningOptions">Optional signing-key configuration.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditServices(
        this IServiceCollection services,
        Action<AuditSigningOptions>? configureSigningOptions = null)
    {
        services.AddOptions<AuditSigningOptions>();
        if (configureSigningOptions is not null)
        {
            services.Configure(configureSigningOptions);
        }

        // Register audit services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITamperEvidentAuditService, TamperEvidentAuditService>();
        services.AddSingleton<ICryptographicSigningService, EcdsaCryptographicSigningService>();

        // Register security audit sub-services
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IAuditReportService, AuditReportService>();

        // Register security audit aggregator facade for backward compatibility
        services.AddScoped<ISecurityAuditAggregator, SecurityAuditAggregator>();

        return services;
    }
}

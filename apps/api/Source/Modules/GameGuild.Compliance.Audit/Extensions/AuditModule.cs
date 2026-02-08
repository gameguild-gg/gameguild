using Microsoft.Extensions.DependencyInjection;

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
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        // Register audit services
        services.AddScoped<IAuditService, AuditService>();

        // Register security audit sub-services
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IAuditReportService, AuditReportService>();

        // Register security audit aggregator facade for backward compatibility
        services.AddScoped<ISecurityAuditAggregator, SecurityAuditAggregator>();

        return services;
    }
}
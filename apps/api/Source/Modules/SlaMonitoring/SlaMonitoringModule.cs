using GameGuild.Modules.SlaMonitoring.Services;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring;

/// <summary>
/// Module registration for SLA/SLO Monitoring.
/// </summary>
public static class SlaMonitoringModule
{
    public static IServiceCollection AddSlaMonitoringModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ISlaMonitoringService, SlaMonitoringService>();
        services.AddScoped<IErrorBudgetCalculator, ErrorBudgetCalculator>();
        services.AddScoped<IAlertManager, AlertManager>();

        // Register repositories
        services.AddScoped<IServiceLevelObjectiveRepository, ServiceLevelObjectiveRepository>();
        services.AddScoped<IServiceLevelIndicatorRepository, ServiceLevelIndicatorRepository>();
        services.AddScoped<ISloViolationRepository, SloViolationRepository>();

        return services;
    }
}

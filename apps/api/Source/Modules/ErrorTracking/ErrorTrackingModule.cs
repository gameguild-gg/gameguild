using GameGuild.Modules.ErrorTracking.Repositories;
using GameGuild.Modules.ErrorTracking.Services;


namespace GameGuild.Modules.ErrorTracking;

/// <summary>
/// Module registration for Error Tracking.
/// </summary>
public static class ErrorTrackingModule
{
    /// <summary>
    /// Register Error Tracking services.
    /// </summary>
    public static IServiceCollection AddErrorTrackingModule(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IErrorTrackingService, ErrorTrackingService>();
        services.AddScoped<IErrorAggregationService, ErrorAggregationService>();

        // Repositories
        services.AddScoped<IErrorEventRepository, ErrorEventRepository>();
        services.AddScoped<IErrorIssueRepository, ErrorIssueRepository>();

        return services;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Analytics;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<AnalyticsWarehouseOptions>(
            configuration.GetSection(AnalyticsWarehouseOptions.SectionName));

        services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();
        services.AddScoped<IKpiDefinitionRepository, KpiDefinitionRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAnalyticsDataWarehouseService, AnalyticsDataWarehouseService>();

        return services;
    }
}

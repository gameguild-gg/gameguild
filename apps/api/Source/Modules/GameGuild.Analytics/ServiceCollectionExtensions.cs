using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.CQRS;

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
        services.AddScoped<IQueryHandler<GetProductMetricsQuery, ProductMetricsResponse>, GetProductMetricsQueryHandler>();
        services.AddScoped<IQueryHandler<ExportProductMetricsQuery, ProductMetricsExportResponse>, ExportProductMetricsQueryHandler>();

        return services;
    }
}

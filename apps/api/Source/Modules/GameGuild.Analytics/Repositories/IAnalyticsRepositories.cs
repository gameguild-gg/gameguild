namespace GameGuild.Analytics;

public interface IAnalyticsEventRepository
{
    Task<AnalyticsEvent> AddAsync(AnalyticsEvent analyticsEvent, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default);
    Task<List<AnalyticsEvent>> GetByEventNameAsync(string eventName, DateTime? startDate, DateTime? endDate, Guid? tenantId, CancellationToken ct = default);
    Task<List<AnalyticsEvent>> GetByUserIdAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<int> CountAsync(string eventName, DateTime? startDate, DateTime? endDate, Guid? tenantId, CancellationToken ct = default);
}

public interface IKpiDefinitionRepository
{
    Task<KpiDefinition?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<KpiDefinition>> GetAllActiveAsync(CancellationToken ct = default);
    Task<KpiDefinition> AddAsync(KpiDefinition kpi, CancellationToken ct = default);
    Task UpdateAsync(KpiDefinition kpi, CancellationToken ct = default);
}

public interface IDashboardRepository
{
    Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Dashboard?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Dashboard>> GetAllAsync(Guid? tenantId, CancellationToken ct = default);
    Task<Dashboard> AddAsync(Dashboard dashboard, CancellationToken ct = default);
    Task UpdateAsync(Dashboard dashboard, CancellationToken ct = default);
}

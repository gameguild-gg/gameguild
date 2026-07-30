
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for SLA impact analysis
/// </summary>
public interface ISlaImpactAnalysisRepository
{
    Task<SlaImpactAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<SlaImpactAnalysis>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<SlaImpactAnalysis>> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<SlaImpactAnalysis>> GetByTypeAsync(ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<SlaImpactAnalysis>> GetByDateRangeAsync(Guid tenantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<SlaImpactAnalysis> CreateAsync(SlaImpactAnalysis analysis, CancellationToken cancellationToken = default);

    Task<SlaImpactAnalysis> AddAsync(SlaImpactAnalysis analysis, CancellationToken cancellationToken = default);

    Task<SlaImpactAnalysis> UpdateAsync(SlaImpactAnalysis analysis, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetViolationCountsByTypeAsync(Guid tenantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<IEnumerable<SlaImpactAnalysis>> GetCriticalOngoingAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<SlaImpactAnalysis>> GetUnresolvedAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

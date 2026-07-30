
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for cost allocation reports
/// </summary>
public interface ICostAllocationReportRepository
{
    Task<CostAllocationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<CostAllocationReport>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<CostAllocationReport>> GetByTenantAsync(Guid tenantId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<IEnumerable<CostAllocationReport>> GetByDateRangeAsync(Guid tenantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<CostAllocationReport> CreateAsync(CostAllocationReport report, CancellationToken cancellationToken = default);

    Task<CostAllocationReport> AddAsync(CostAllocationReport report, CancellationToken cancellationToken = default);

    Task<CostAllocationReport> UpdateAsync(CostAllocationReport report, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<CostAllocationReport>> GetUnexportedReportsAsync(CancellationToken cancellationToken = default);
}

using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Abstractions;

/// <summary>
///     Repository interface for managing usage records
/// </summary>
public interface IUsageRecordRepository
{
    Task<UsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTenantAsync(Guid tenantId, ResourceUsageType? type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByDateRangeAsync(Guid tenantId, ResourceUsageType type, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<UsageRecord> CreateAsync(UsageRecord record, CancellationToken cancellationToken = default);

    Task<UsageRecord> AddAsync(UsageRecord record, CancellationToken cancellationToken = default);

    Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<bool> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    Task<bool> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<long> CalculateTotalUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<int> ArchiveOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    Task<bool> DeleteByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<bool> DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

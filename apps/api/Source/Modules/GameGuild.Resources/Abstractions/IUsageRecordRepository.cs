

namespace GameGuild.Resources;

/// <summary>
///     Repository interface for managing usage records
/// </summary>
public interface IUsageRecordRepository
{
    Task<UsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTenantAsync(Guid tenantId, ResourceUsageType? type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Gets paginated usage records for a tenant with optional filters
    /// </summary>
    Task<PagedResult<UsageRecord>> GetPagedByTenantAsync(
        Guid tenantId, 
        ResourceUsageType? type = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByTypeAsync(ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByDateRangeAsync(Guid tenantId, ResourceUsageType type, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<UsageRecord> CreateAsync(UsageRecord record, CancellationToken cancellationToken = default);

    Task<UsageRecord> AddAsync(UsageRecord record, CancellationToken cancellationToken = default);

    Task<UsageRecord> UpdateAsync(UsageRecord record, CancellationToken cancellationToken = default);

    Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<bool> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    Task<bool> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<long> CalculateTotalUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<int> ArchiveOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    Task<bool> DeleteByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<bool> DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    // User-level methods
    Task<IEnumerable<UsageRecord>> GetByUserAsync(Guid userId, ResourceUsageType? type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRecord>> GetByUserDateRangeAsync(Guid userId, ResourceUsageType type, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<long> GetCurrentUserUsageAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<bool> DeleteByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default);

    // Stats methods for retention tracking
    Task<int> GetTotalRecordCountAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    Task<int> GetArchivedRecordCountAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    Task<DateTime?> GetOldestRecordDateAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    Task<long> GetEstimatedStorageBytesAsync(Guid? tenantId = null, bool archivedOnly = false, CancellationToken cancellationToken = default);
}

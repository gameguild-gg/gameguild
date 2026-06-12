
namespace GameGuild.Resources;

/// <summary>
///     Service for managing usage data retention and archival
/// </summary>
public interface IUsageRetentionService
{
    /// <summary>
    ///     Create or update a retention policy
    /// </summary>
    Task<UsageRetentionPolicy> SetPolicyAsync(Guid? tenantId, ResourceUsageType? resourceType, int retentionDays, int archiveAfterDays, bool enableCompaction = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get retention policy
    /// </summary>
    Task<UsageRetentionPolicy?> GetPolicyAsync(Guid? tenantId, ResourceUsageType? resourceType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all active policies
    /// </summary>
    Task<IEnumerable<UsageRetentionPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Execute retention policy (archive old data)
    /// </summary>
    Task<RetentionExecutionResult> ExecuteRetentionAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Compact usage records (aggregate to reduce storage)
    /// </summary>
    Task<int> CompactUsageRecordsAsync(Guid tenantId, ResourceUsageType? type = null, DateTime? olderThan = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Archive usage records to cold storage
    /// </summary>
    Task<int> ArchiveUsageRecordsAsync(Guid tenantId, ResourceUsageType? type = null, DateTime? olderThan = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete archived records permanently
    /// </summary>
    Task<int> DeleteArchivedRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get retention statistics
    /// </summary>
    Task<RetentionStats> GetRetentionStatsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
}

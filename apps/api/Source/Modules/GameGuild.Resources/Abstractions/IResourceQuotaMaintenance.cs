namespace GameGuild.Resources;

/// <summary>
///     Maintenance operations for resource quotas.
///     Use this interface for background jobs and system maintenance tasks.
/// </summary>
/// <remarks>
///     Part of the ISP-compliant split of IResourceQuotaService.
///     Only background job services should depend on this interface.
/// </remarks>
public interface IResourceQuotaMaintenance
{
    /// <summary>
    ///     Reset quotas that are due for reset based on their period
    /// </summary>
    Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clean up old usage records
    /// </summary>
    Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Recalculate current usage from usage records
    /// </summary>
    Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);
}

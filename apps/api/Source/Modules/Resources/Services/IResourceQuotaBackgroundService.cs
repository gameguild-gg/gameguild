namespace GameGuild.Modules.Resources.Services;

/// <summary>
/// Background service for automated resource quota operations
/// </summary>
public interface IResourceQuotaBackgroundService
{
    /// <summary>
    /// Resets quotas that are due for reset based on their period
    /// </summary>
    Task ResetDueQuotasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old usage records older than retention period
    /// </summary>
    Task ArchiveOldUsageRecordsAsync(int retentionDays = 90, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks quotas approaching limits and generates notifications
    /// </summary>
    Task CheckQuotaThresholdsAsync(CancellationToken cancellationToken = default);
}

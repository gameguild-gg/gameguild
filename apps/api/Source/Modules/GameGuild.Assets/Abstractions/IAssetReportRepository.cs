namespace GameGuild.Assets;

/// <summary>
/// Repository for AssetReport entities.
/// </summary>
public interface IAssetReportRepository
{
    /// <summary>
    /// Gets a report by ID.
    /// </summary>
    Task<AssetReport?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets reports for an asset reference.
    /// </summary>
    Task<IReadOnlyList<AssetReport>> GetByAssetReferenceAsync(Guid assetReferenceId, CancellationToken ct = default);

    /// <summary>
    /// Gets pending reports for moderation queue.
    /// </summary>
    Task<IReadOnlyList<AssetReport>> GetPendingReportsAsync(int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Adds a new report.
    /// </summary>
    Task<AssetReport> AddAsync(AssetReport report, CancellationToken ct = default);

    /// <summary>
    /// Updates a report.
    /// </summary>
    Task UpdateAsync(AssetReport report, CancellationToken ct = default);

    /// <summary>
    /// Checks if user has already reported an asset.
    /// </summary>
    Task<bool> HasUserReportedAsync(Guid assetReferenceId, Guid userId, CancellationToken ct = default);
}

namespace GameGuild.Assets;

/// <summary>
/// Repository for TransformedAsset entities.
/// </summary>
public interface ITransformedAssetRepository
{
    /// <summary>
    /// Gets a transformed asset by source content and transformation spec.
    /// </summary>
    Task<TransformedAsset?> GetAsync(
        Guid sourceContentId,
        string transformationSpec,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a new transformed asset.
    /// </summary>
    Task<TransformedAsset> AddAsync(TransformedAsset asset, CancellationToken ct = default);

    /// <summary>
    /// Updates a transformed asset (e.g., last accessed time).
    /// </summary>
    Task UpdateAsync(TransformedAsset asset, CancellationToken ct = default);

    /// <summary>
    /// Gets stale transformed assets for cache eviction.
    /// </summary>
    Task<IReadOnlyList<TransformedAsset>> GetStaleAssetsAsync(
        TimeSpan maxAge,
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a transformed asset.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes all transformed assets for a source content.
    /// </summary>
    Task DeleteBySourceAsync(Guid sourceContentId, CancellationToken ct = default);
}

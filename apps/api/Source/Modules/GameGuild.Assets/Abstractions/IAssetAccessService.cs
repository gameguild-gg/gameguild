namespace GameGuild.Assets;

/// <summary>
/// Service for generating access URLs for assets.
/// </summary>
public interface IAssetAccessService
{
    /// <summary>
    /// Generates an access URL for an asset.
    /// </summary>
    Task<AssetAccessUrl> GenerateAccessUrlAsync(
        Guid assetReferenceId,
        Guid userId,
        TransformationSpec? transformation = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validates access to an asset.
    /// </summary>
    Task<AssetAccessValidation> ValidateAccessAsync(
        Guid assetReferenceId,
        string token,
        CancellationToken ct = default);

    /// <summary>
    /// Records an access to an asset.
    /// </summary>
    Task RecordAccessAsync(Guid assetReferenceId, CancellationToken ct = default);
}

/// <summary>
/// Generated access URL for an asset.
/// </summary>
public record AssetAccessUrl(
    string Url,
    DateTime ExpiresAt,
    bool RequiresTransformation);

/// <summary>
/// Result of access validation.
/// </summary>
public record AssetAccessValidation(
    bool IsValid,
    AssetAccessDeniedReason? DeniedReason,
    AssetReference? Reference,
    TransformationSpec? Transformation);

/// <summary>
/// Reason for access denial.
/// </summary>
public enum AssetAccessDeniedReason
{
    NotFound,
    TokenInvalid,
    TokenExpired,
    RateLimitExceeded,
    ContentRejected,
    ContentInfected,
    FeatureDisabled,
    InsufficientPermission,
    DownloadWindowExpired
}

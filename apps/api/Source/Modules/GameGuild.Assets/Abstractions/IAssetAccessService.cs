namespace GameGuild.Assets;

/// <summary>
/// Service for generating access URLs for assets.
/// </summary>
public interface IAssetAccessService
{
    /// <summary>
    /// Generates an access URL for an asset.
    /// </summary>
    Task<AssetAccessUrl?> GenerateAccessUrlAsync(
        Guid assetReferenceId,
        Guid? userId,
        Guid? tenantId,
        TransformationSpec? transformation = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a direct storage presigned URL (bypasses token system).
    /// </summary>
    Task<AssetAccessUrl?> GenerateDirectStorageUrlAsync(
        Guid assetReferenceId,
        Guid? userId,
        Guid? tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates access to an asset.
    /// </summary>
    Task<AssetAccessValidation> ValidateAccessAsync(
        Guid assetReferenceId,
        Guid? userId,
        Guid? tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a token for asset access.
    /// </summary>
    bool ValidateToken(
        string token,
        Guid assetReferenceId,
        Guid? tenantId);
}

/// <summary>
/// Generated access URL for an asset.
/// </summary>
public record AssetAccessUrl(
    string Url,
    string Token,
    DateTimeOffset ExpiresAt,
    string MimeType);

/// <summary>
/// Result of access validation.
/// </summary>
public record AssetAccessValidation(
    bool IsValid,
    AssetAccessDeniedReason? DeniedReason);

/// <summary>
/// Reason for access denial.
/// </summary>
public enum AssetAccessDeniedReason
{
    NotFound,
    TokenInvalid,
    TokenExpired,
    AuthenticationRequired,
    OwnershipRequired,
    InvalidPolicy,
    ContentRejected,
    ContentInfected
}

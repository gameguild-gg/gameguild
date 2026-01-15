namespace GameGuild.Assets;

/// <summary>
/// Service for generating and validating signed asset access tokens.
/// </summary>
public interface IAssetTokenService
{
    /// <summary>
    /// Generates a signed access token for an asset.
    /// </summary>
    string GenerateToken(
        Guid assetReferenceId,
        Guid tenantId,
        AssetAccessPolicy accessPolicy,
        TransformationSpec? transformation = null,
        TimeSpan? customExpiry = null);

    /// <summary>
    /// Validates a token and returns the decoded payload if valid.
    /// </summary>
    AssetTokenPayload? ValidateToken(string token, Guid assetReferenceId, Guid tenantId);

    /// <summary>
    /// Gets the current time window index.
    /// </summary>
    int GetCurrentTimeWindow();
}

/// <summary>
/// Decoded payload from an asset access token.
/// </summary>
public sealed record AssetTokenPayload(
    Guid AssetReferenceId,
    int TimeWindow,
    long ExpiryTimestamp,
    AssetAccessPolicy AccessPolicy,
    string TransformationSpec,
    Guid TenantId);

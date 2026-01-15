using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameGuild.Features;

namespace GameGuild.Assets;

/// <summary>
/// Options for asset access URLs.
/// </summary>
public class AssetAccessOptions
{
    public const string SectionName = "Assets:Access";

    public string BaseUrl { get; set; } = string.Empty;
    public int DefaultExpiryMinutes { get; set; } = 60;
    public bool UsePresignedUrls { get; set; } = true;
}

/// <summary>
/// Implementation of access URL generation for assets.
/// </summary>
public class AssetAccessService : IAssetAccessService
{
    private readonly IAssetReferenceRepository _referenceRepository;
    private readonly IAssetStorageService _storageService;
    private readonly IAssetTokenService _tokenService;
    private readonly IFeatureFlagEvaluationService _featureService;
    private readonly AssetAccessOptions _options;
    private readonly ILogger<AssetAccessService> _logger;

    public AssetAccessService(
        IAssetReferenceRepository referenceRepository,
        IAssetStorageService storageService,
        IAssetTokenService tokenService,
        IFeatureFlagEvaluationService featureService,
        IOptions<AssetAccessOptions> options,
        ILogger<AssetAccessService> logger)
    {
        _referenceRepository = referenceRepository;
        _storageService = storageService;
        _tokenService = tokenService;
        _featureService = featureService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AssetAccessUrl?> GenerateAccessUrlAsync(
        Guid assetReferenceId,
        Guid? userId,
        Guid? tenantId,
        TransformationSpec? transformation = null,
        CancellationToken ct = default)
    {
        var reference = await _referenceRepository.GetByIdWithContentAsync(assetReferenceId, ct);
        if (reference == null || reference.Content == null)
        {
            return null;
        }

        // Check access policy
        var validation = await ValidateAccessAsync(assetReferenceId, userId, tenantId, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Access denied to asset {AssetId} for user {UserId}: {Reason}",
                assetReferenceId, userId, validation.DeniedReason);
            return null;
        }

        // Check content status
        if (reference.Content.VirusScanStatus == VirusScanStatus.Infected ||
            reference.Content.ModerationStatus == ModerationStatus.Blocked)
        {
            return null;
        }

        // Feature flag check for transformations
        if (transformation != null && !transformation.IsIdentity)
        {
            var featureContext = CreateFeatureContext(userId, tenantId);
            var transformationsEnabled = await _featureService.IsEnabledAsync(
                FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled,
                featureContext,
                ct);

            if (!transformationsEnabled)
            {
                _logger.LogDebug(
                    "Transformations disabled by feature flag for tenant {TenantId}",
                    tenantId);
                transformation = null; // Fall back to original
            }
            else
            {
                // Check max dimension limit
                var maxDimension = await _featureService.GetValueAsync<int>(
                    FeatureFlagConstants.AssetFeatureFlags.MaxTransformDimension,
                    featureContext,
                    4096, // Default max dimension
                    ct);

                if ((transformation.Width.HasValue && transformation.Width > maxDimension) ||
                    (transformation.Height.HasValue && transformation.Height > maxDimension))
                {
                    _logger.LogWarning(
                        "Transformation dimensions exceed limit {MaxDimension} for tenant {TenantId}",
                        maxDimension, tenantId);
                    return null;
                }
            }
        }

        // Get download window from feature flag
        var downloadWindowHours = await GetDownloadWindowHoursAsync(userId, tenantId, ct);
        var expiryMinutes = downloadWindowHours * 60;

        // Generate token
        var token = _tokenService.GenerateToken(
            assetReferenceId,
            tenantId ?? Guid.Empty,
            reference.AccessPolicy,
            transformation,
            TimeSpan.FromMinutes(expiryMinutes));

        var url = BuildAccessUrl(assetReferenceId, token, transformation);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        // Record access
        await _referenceRepository.RecordAccessAsync(assetReferenceId, ct);

        return new AssetAccessUrl(url, token, expiry, reference.Content.MimeType);
    }

    /// <summary>
    /// Gets the download window hours from feature flags.
    /// </summary>
    private async Task<int> GetDownloadWindowHoursAsync(
        Guid? userId,
        Guid? tenantId,
        CancellationToken ct)
    {
        var featureContext = CreateFeatureContext(userId, tenantId);
        return await _featureService.GetValueAsync<int>(
            FeatureFlagConstants.AssetFeatureFlags.DownloadWindowHours,
            featureContext,
            _options.DefaultExpiryMinutes / 60, // Fall back to config
            ct);
    }

    /// <summary>
    /// Creates a feature evaluation context.
    /// </summary>
    private static FeatureContext CreateFeatureContext(Guid? userId, Guid? tenantId)
    {
        return new FeatureContext
        {
            UserId = userId,
            TenantId = tenantId
        };
    }

    public async Task<AssetAccessUrl?> GenerateDirectStorageUrlAsync(
        Guid assetReferenceId,
        Guid? userId,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var reference = await _referenceRepository.GetByIdWithContentAsync(assetReferenceId, ct);
        if (reference == null || reference.Content == null)
        {
            return null;
        }

        // Check access
        var validation = await ValidateAccessAsync(assetReferenceId, userId, tenantId, ct);
        if (!validation.IsValid)
        {
            return null;
        }

        // Generate presigned URL directly from storage
        var expiry = TimeSpan.FromMinutes(_options.DefaultExpiryMinutes);
        var presignedUrl = await _storageService.GeneratePresignedUrlAsync(
            reference.Content.BucketName,
            reference.Content.ObjectKey,
            expiry,
            isDownload: true,
            ct);

        await _referenceRepository.RecordAccessAsync(assetReferenceId, ct);

        return new AssetAccessUrl(
            presignedUrl,
            string.Empty, // No token for direct access
            DateTimeOffset.UtcNow.Add(expiry),
            reference.Content.MimeType);
    }

    public async Task<AssetAccessValidation> ValidateAccessAsync(
        Guid assetReferenceId,
        Guid? userId,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var reference = await _referenceRepository.GetByIdAsync(assetReferenceId, ct);
        if (reference == null)
        {
            return new AssetAccessValidation(false, AssetAccessDeniedReason.NotFound);
        }

        // Check if soft-deleted
        if (reference.IsDeleted)
        {
            return new AssetAccessValidation(false, AssetAccessDeniedReason.NotFound);
        }

        // Check access policy
        switch (reference.AccessPolicy)
        {
            case AssetAccessPolicy.Public:
                return new AssetAccessValidation(true, null);

            case AssetAccessPolicy.Unlisted:
                // Unlisted assets are accessible to anyone with the URL
                return new AssetAccessValidation(true, null);

            case AssetAccessPolicy.Authenticated:
                if (userId == null)
                {
                    return new AssetAccessValidation(false, AssetAccessDeniedReason.AuthenticationRequired);
                }
                return new AssetAccessValidation(true, null);

            case AssetAccessPolicy.OwnerOnly:
                if (userId == null)
                {
                    return new AssetAccessValidation(false, AssetAccessDeniedReason.AuthenticationRequired);
                }
                if (reference.CreatedByUserId != userId)
                {
                    return new AssetAccessValidation(false, AssetAccessDeniedReason.OwnershipRequired);
                }
                return new AssetAccessValidation(true, null);

            case AssetAccessPolicy.Inherited:
                // TODO: Check parent resource access
                // For now, require authentication
                if (userId == null)
                {
                    return new AssetAccessValidation(false, AssetAccessDeniedReason.AuthenticationRequired);
                }
                return new AssetAccessValidation(true, null);

            default:
                return new AssetAccessValidation(false, AssetAccessDeniedReason.InvalidPolicy);
        }
    }

    public bool ValidateToken(
        string token,
        Guid assetReferenceId,
        Guid? tenantId)
    {
        var payload = _tokenService.ValidateToken(token, assetReferenceId, tenantId ?? Guid.Empty);
        return payload != null;
    }

    private string BuildAccessUrl(
        Guid assetReferenceId,
        string token,
        TransformationSpec? transformation)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/api/assets/{assetReferenceId}/content?token={token}";

        if (transformation != null && !transformation.IsIdentity)
        {
            url += $"&transform={Uri.EscapeDataString(transformation.ToCanonicalString())}";
        }

        return url;
    }

    /// <inheritdoc />
    public async Task<TokenValidationResult> ValidateAccessTokenAsync(
        Guid assetReferenceId,
        string token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new TokenValidationResult(false, "Token is required");
        }

        // Validate token signature and expiration
        var payload = _tokenService.ValidateToken(token, assetReferenceId, null);
        if (payload == null)
        {
            return new TokenValidationResult(false, "Invalid or expired token");
        }

        // Check that the asset still exists
        var reference = await _referenceRepository.GetByIdAsync(assetReferenceId, ct);
        if (reference == null || reference.IsDeleted)
        {
            return new TokenValidationResult(false, "Asset not found");
        }

        return new TokenValidationResult(
            true,
            null,
            payload.UserId,
            payload.ExpiresAt);
    }

    /// <inheritdoc />
    public Task<EphemeralTokenValidationResult> ValidateEphemeralTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(new EphemeralTokenValidationResult(
                false, Guid.Empty, false, "Token is required"));
        }

        // Decode ephemeral token (format: base64({assetId}:{expiry}:{signature}))
        var ephemeralPayload = _tokenService.ValidateEphemeralToken(token);
        if (ephemeralPayload == null)
        {
            return Task.FromResult(new EphemeralTokenValidationResult(
                false, Guid.Empty, false, "Invalid ephemeral token"));
        }

        if (ephemeralPayload.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Task.FromResult(new EphemeralTokenValidationResult(
                false, ephemeralPayload.AssetReferenceId, true, "Token has expired"));
        }

        return Task.FromResult(new EphemeralTokenValidationResult(
            true, ephemeralPayload.AssetReferenceId));
    }

    /// <inheritdoc />
    public async Task<TransformedAssetInfo?> GetOrCreateTransformationAsync(
        Guid contentId,
        TransformationSpec spec,
        CancellationToken ct = default)
    {
        // Check feature flag for transformations
        var featureContext = CreateFeatureContext(null, null);
        var transformationsEnabled = await _featureService.IsEnabledAsync(
            FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled,
            featureContext,
            ct);

        if (!transformationsEnabled)
        {
            _logger.LogWarning("Transformations disabled by feature flag");
            return null;
        }

        // For now, return null - transformation implementation is in TransformationService
        // This is a placeholder that would delegate to the transformation service
        _logger.LogDebug(
            "GetOrCreateTransformation called for content {ContentId} with spec {Spec}",
            contentId, spec);

        // TODO: Implement transformation lookup/creation via ITransformationService
        // var transformedAsset = await _transformationService.GetOrCreateAsync(contentId, spec, ct);
        // return transformedAsset != null
        //     ? new TransformedAssetInfo(transformedAsset.Id, contentId, ...)
        //     : null;

        return null;
    }
}

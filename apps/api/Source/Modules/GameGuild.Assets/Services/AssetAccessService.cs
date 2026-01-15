using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly AssetAccessOptions _options;
    private readonly ILogger<AssetAccessService> _logger;

    public AssetAccessService(
        IAssetReferenceRepository referenceRepository,
        IAssetStorageService storageService,
        IAssetTokenService tokenService,
        IOptions<AssetAccessOptions> options,
        ILogger<AssetAccessService> logger)
    {
        _referenceRepository = referenceRepository;
        _storageService = storageService;
        _tokenService = tokenService;
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

        // Generate token
        var token = _tokenService.GenerateToken(
            assetReferenceId,
            tenantId ?? Guid.Empty,
            reference.AccessPolicy,
            transformation,
            TimeSpan.FromMinutes(_options.DefaultExpiryMinutes));

        var url = BuildAccessUrl(assetReferenceId, token, transformation);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(_options.DefaultExpiryMinutes);

        // Record access
        await _referenceRepository.RecordAccessAsync(assetReferenceId, ct);

        return new AssetAccessUrl(url, token, expiry, reference.Content.MimeType);
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
}

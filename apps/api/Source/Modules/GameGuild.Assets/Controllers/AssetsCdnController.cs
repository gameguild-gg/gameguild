using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Assets.Controllers;

/// <summary>
/// CDN-optimized routes for serving assets.
/// These routes use path-based tokens instead of query strings for better CDN caching.
/// </summary>
/// <remarks>
/// Route patterns:
/// - /assets/{referenceId}/{token} - Direct asset access with path token
/// - /e/{token} - Ephemeral URL with embedded asset reference
/// - /t/{transformation}/{referenceId}/{token} - Transformed asset access
/// </remarks>
[ApiController]
[Route("assets")]
[AllowAnonymous] // Token-based authorization, not session-based
public class AssetsCdnController : ControllerBase
{
    private readonly IAssetAccessService _accessService;
    private readonly IAssetStorageService _storageService;
    private readonly IAssetContentRepository _contentRepository;
    private readonly IAssetReferenceRepository _referenceRepository;

    public AssetsCdnController(
        IAssetAccessService accessService,
        IAssetStorageService storageService,
        IAssetContentRepository contentRepository,
        IAssetReferenceRepository referenceRepository)
    {
        _accessService = accessService;
        _storageService = storageService;
        _contentRepository = contentRepository;
        _referenceRepository = referenceRepository;
    }

    /// <summary>
    /// Serve asset content with path-based token (CDN-friendly).
    /// </summary>
    /// <remarks>
    /// URL format: /assets/{referenceId}/{token}
    /// This format is more CDN-friendly than query-string tokens because:
    /// - Path-based URLs are consistently cached
    /// - No query string parsing issues
    /// - Works with CDNs that strip query strings
    /// </remarks>
    [HttpGet("{referenceId:guid}/{token}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)] // 24 hour cache
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAssetWithPathToken(
        Guid referenceId,
        string token,
        CancellationToken ct = default)
    {
        // Validate token
        var validation = await _accessService.ValidateAccessTokenAsync(referenceId, token, ct);
        if (!validation.IsValid)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Access Denied",
                Detail = validation.Error ?? "Invalid or expired access token"
            });
        }

        // Get asset reference
        var reference = await _referenceRepository.GetByIdAsync(referenceId, ct);
        if (reference == null)
        {
            return NotFound();
        }

        // Get content
        var content = await _contentRepository.GetByIdAsync(reference.ContentId, ct);
        if (content == null)
        {
            return NotFound();
        }

        // Stream content
        var stream = await _storageService.GetAsync(content.BucketName, content.ObjectKey, ct);
        if (stream == null)
        {
            return NotFound();
        }

        // Set cache headers for CDN
        Response.Headers.CacheControl = "public, max-age=86400"; // 24 hours
        Response.Headers.ETag = $"\"{content.ContentHash}\"";

        return File(stream, content.MimeType, reference.DisplayName);
    }

    /// <summary>
    /// Serve ephemeral asset (short-lived URL with embedded reference).
    /// </summary>
    /// <remarks>
    /// URL format: /e/{token}
    /// The token contains the encrypted asset reference ID and expiration.
    /// Useful for temporary share links and secure downloads.
    /// </remarks>
    [HttpGet("/e/{token}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // 5 minute cache (ephemeral)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> GetEphemeralAsset(
        string token,
        CancellationToken ct = default)
    {
        // Decode ephemeral token (contains asset ID and expiration)
        var ephemeralInfo = await _accessService.ValidateEphemeralTokenAsync(token, ct);
        if (!ephemeralInfo.IsValid)
        {
            if (ephemeralInfo.IsExpired)
            {
                return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
                {
                    Title = "Link Expired",
                    Detail = "This download link has expired"
                });
            }

            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Access Denied",
                Detail = ephemeralInfo.Error ?? "Invalid ephemeral token"
            });
        }

        // Get asset reference
        var reference = await _referenceRepository.GetByIdAsync(ephemeralInfo.AssetReferenceId, ct);
        if (reference == null)
        {
            return NotFound();
        }

        // Get content
        var content = await _contentRepository.GetByIdAsync(reference.ContentId, ct);
        if (content == null)
        {
            return NotFound();
        }

        // Stream content
        var stream = await _storageService.GetAsync(content.BucketName, content.ObjectKey, ct);
        if (stream == null)
        {
            return NotFound();
        }

        // Set cache headers (shorter for ephemeral)
        Response.Headers.CacheControl = "private, max-age=300"; // 5 minutes

        return File(stream, content.MimeType, reference.DisplayName);
    }

    /// <summary>
    /// Serve transformed asset (resized, cropped, etc.) with CDN caching.
    /// </summary>
    /// <remarks>
    /// URL format: /t/{transformation}/{referenceId}/{token}
    /// Transformations: 
    /// - thumb_100x100 - Thumbnail
    /// - resize_800x600 - Resize
    /// - crop_center_400x400 - Center crop
    /// </remarks>
    [HttpGet("/t/{transformation}/{referenceId:guid}/{token}")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)] // 7 day cache for transforms
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTransformedAsset(
        string transformation,
        Guid referenceId,
        string token,
        CancellationToken ct = default)
    {
        // Validate token
        var validation = await _accessService.ValidateAccessTokenAsync(referenceId, token, ct);
        if (!validation.IsValid)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Access Denied",
                Detail = validation.Error ?? "Invalid or expired access token"
            });
        }

        // Parse transformation
        var transformSpec = ParseTransformation(transformation);
        if (transformSpec == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Transformation",
                Detail = $"Unknown transformation: {transformation}"
            });
        }

        // Get asset reference
        var reference = await _referenceRepository.GetByIdAsync(referenceId, ct);
        if (reference == null)
        {
            return NotFound();
        }

        // Check if transformation already exists
        var transformedAsset = await _accessService.GetOrCreateTransformationAsync(
            reference.ContentId,
            transformSpec,
            ct);

        if (transformedAsset == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transformation Failed",
                Detail = "Unable to generate transformed asset"
            });
        }

        // Stream transformed content
        var stream = await _storageService.GetAsync(
            transformedAsset.BucketName,
            transformedAsset.ObjectKey,
            ct);

        if (stream == null)
        {
            return NotFound();
        }

        // Set aggressive cache headers for transformed assets
        Response.Headers.CacheControl = "public, max-age=604800, immutable"; // 7 days, immutable
        Response.Headers.ETag = $"\"{transformedAsset.ContentHash}\"";

        return File(stream, transformedAsset.MimeType);
    }

    private static TransformationSpec? ParseTransformation(string transformation)
    {
        if (string.IsNullOrEmpty(transformation))
            return null;

        var parts = transformation.Split('_');
        if (parts.Length < 2)
            return null;

        return parts[0].ToLowerInvariant() switch
        {
            "thumb" when TryParseSize(parts[1], out var w1, out var h1) =>
                new TransformationSpec(TransformationType.Thumbnail, w1, h1),
            "resize" when TryParseSize(parts[1], out var w2, out var h2) =>
                new TransformationSpec(TransformationType.Resize, w2, h2),
            "crop" when parts.Length >= 3 && TryParseSize(parts[2], out var w3, out var h3) =>
                new TransformationSpec(TransformationType.Crop, w3, h3, parts[1]),
            _ => null
        };
    }

    private static bool TryParseSize(string size, out int width, out int height)
    {
        width = 0;
        height = 0;

        var dimensions = size.Split('x');
        if (dimensions.Length != 2)
            return false;

        return int.TryParse(dimensions[0], out width) &&
               int.TryParse(dimensions[1], out height) &&
               width > 0 && width <= 4096 &&
               height > 0 && height <= 4096;
    }
}

/// <summary>
/// Specification for an asset transformation.
/// </summary>
public record TransformationSpec(
    TransformationType Type,
    int Width,
    int Height,
    string? Anchor = null);

/// <summary>
/// Types of transformations supported.
/// </summary>
public enum TransformationType
{
    /// <summary>Generate a thumbnail (crop to fit)</summary>
    Thumbnail,
    
    /// <summary>Resize maintaining aspect ratio</summary>
    Resize,
    
    /// <summary>Crop to exact dimensions</summary>
    Crop,
    
    /// <summary>Convert to different format</summary>
    Convert
}

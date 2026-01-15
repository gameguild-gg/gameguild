using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;
using GameGuild.Identity.Context;

namespace GameGuild.Assets.Controllers;

/// <summary>
/// Controller for asset operations.
/// </summary>
[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IActorContext _actorContext;

    public AssetsController(
        IRequestDispatcher dispatcher,
        IActorContext actorContext)
    {
        _dispatcher = dispatcher;
        _actorContext = actorContext;
    }

    /// <summary>
    /// Upload a new asset.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] string? displayName = null,
        [FromQuery] AssetAccessPolicy accessPolicy = AssetAccessPolicy.Private,
        [FromQuery] string? parentResourceType = null,
        [FromQuery] Guid? parentResourceId = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "No file provided" });
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadAssetCommand(
            stream,
            file.FileName,
            file.ContentType,
            _actorContext.UserId,
            _actorContext.TenantId,
            displayName,
            accessPolicy,
            parentResourceType,
            parentResourceId);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return CreatedAtAction(
            nameof(GetAsset),
            new { id = result.Value!.AssetReferenceId },
            result.Value);
    }

    /// <summary>
    /// Get an asset by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsset(
        Guid id,
        [FromQuery] bool includeContent = false,
        CancellationToken ct = default)
    {
        var query = new GetAssetQuery(
            id,
            _actorContext.IsAuthenticated ? _actorContext.UserId : null,
            _actorContext.TenantId,
            includeContent);

        var result = await _dispatcher.DispatchAsync(query, ct);

        if (!result.IsSuccess)
        {
            return Forbid();
        }

        if (result.Value == null)
        {
            return NotFound();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Generate an access URL for an asset.
    /// </summary>
    [HttpPost("{id:guid}/access-url")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateAccessUrl(
        Guid id,
        [FromQuery] int? width = null,
        [FromQuery] int? height = null,
        [FromQuery] ImageFit? fit = null,
        [FromQuery] ImageFormat? format = null,
        [FromQuery] int? quality = null,
        [FromQuery] bool direct = false,
        CancellationToken ct = default)
    {
        TransformationSpec? transformation = null;
        if (width.HasValue || height.HasValue || fit.HasValue || format.HasValue || quality.HasValue)
        {
            transformation = new TransformationSpec(
                width,
                height,
                fit ?? ImageFit.Contain,
                format ?? ImageFormat.Original,
                quality ?? 85);
        }

        var command = new GenerateAccessUrlCommand(
            id,
            _actorContext.IsAuthenticated ? _actorContext.UserId : null,
            _actorContext.TenantId,
            transformation,
            direct);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get asset content (serve the actual file).
    /// </summary>
    [HttpGet("{id:guid}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContent(
        Guid id,
        [FromQuery] string token,
        [FromQuery] string? transform = null,
        [FromServices] IAssetAccessService accessService,
        [FromServices] IAssetStorageService storageService,
        [FromServices] IAssetReferenceRepository referenceRepository,
        CancellationToken ct = default)
    {
        // Validate token
        if (!accessService.ValidateToken(token, id, _actorContext.TenantId))
        {
            return Forbid();
        }

        var reference = await referenceRepository.GetByIdWithContentAsync(id, ct);
        if (reference?.Content == null)
        {
            return NotFound();
        }

        // Check content status
        if (reference.Content.VirusScanStatus == VirusScanStatus.Infected ||
            reference.Content.ModerationStatus == ModerationStatus.Blocked)
        {
            return Forbid();
        }

        var stream = await storageService.DownloadAsync(
            reference.Content.BucketName,
            reference.Content.ObjectKey,
            ct);

        await referenceRepository.RecordAccessAsync(id, ct);

        return File(stream, reference.Content.MimeType, reference.DisplayName);
    }

    /// <summary>
    /// Update asset metadata.
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateAsset(
        Guid id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken ct = default)
    {
        var command = new UpdateAssetCommand(
            id,
            _actorContext.UserId,
            request.DisplayName,
            request.AccessPolicy);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete an asset.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsset(
        Guid id,
        CancellationToken ct = default)
    {
        var command = new DeleteAssetCommand(id, _actorContext.UserId);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Report an asset for moderation.
    /// </summary>
    [HttpPost("{id:guid}/report")]
    public async Task<IActionResult> ReportAsset(
        Guid id,
        [FromBody] ReportAssetRequest request,
        CancellationToken ct = default)
    {
        var command = new ReportAssetCommand(
            id,
            _actorContext.UserId,
            request.Reason,
            request.Description);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get assets for the current user.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAssets(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var query = new GetUserAssetsQuery(
            _actorContext.UserId,
            _actorContext.TenantId,
            skip,
            take);

        var result = await _dispatcher.DispatchAsync(query, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get assets by parent resource.
    /// </summary>
    [HttpGet("by-parent")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByParent(
        [FromQuery] string parentType,
        [FromQuery] Guid parentId,
        CancellationToken ct = default)
    {
        var query = new GetAssetsByParentQuery(
            parentType,
            parentId,
            _actorContext.IsAuthenticated ? _actorContext.UserId : null,
            _actorContext.TenantId);

        var result = await _dispatcher.DispatchAsync(query, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
    }
}

public record UpdateAssetRequest(
    string? DisplayName = null,
    AssetAccessPolicy? AccessPolicy = null);

public record ReportAssetRequest(
    ReportReason Reason,
    string? Description = null);

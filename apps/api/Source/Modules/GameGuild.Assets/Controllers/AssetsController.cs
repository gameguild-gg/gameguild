using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Assets.Controllers;

/// <summary>
/// Controller for asset operations.
/// </summary>
[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : ControllerBase
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

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

        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadAssetCommand(
            stream,
            file.FileName,
            file.ContentType,
            Actor.SubjectIdAsGuid.Value,
            Actor.TenantId,
            displayName,
            accessPolicy,
            parentResourceType,
            parentResourceId);

        var result = await sender.Send(command, ct);

        if (result.Error != null)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return CreatedAtAction(
            nameof(GetAsset),
            new { id = result.AssetReferenceId },
            result);
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
            Actor.SubjectIdAsGuid,
            Actor.TenantId,
            includeContent);

        var result = await sender.Send(query, ct);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
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
            transformation = new TransformationSpec
            {
                Width = width,
                Height = height,
                Fit = fit ?? ImageFit.Contain,
                Format = format ?? ImageFormat.Original,
                Quality = quality ?? 85
            };
        }

        var command = new GenerateAccessUrlCommand(
            id,
            Actor.SubjectIdAsGuid,
            Actor.TenantId,
            transformation,
            direct);

        var result = await sender.Send(command, ct);

        if (result == null)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to generate access URL" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get asset content (serve the actual file).
    /// </summary>
    [HttpGet("{id:guid}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContent(
        Guid id,
        [FromQuery] string token,
        [FromServices] IAssetAccessService accessService,
        [FromServices] IAssetStorageService storageService,
        [FromServices] IAssetReferenceRepository referenceRepository,
        [FromQuery] string? transform = null,
        CancellationToken ct = default)
    {
        // Validate token
        if (!accessService.ValidateToken(token, id, Actor.TenantId))
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
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var command = new UpdateAssetCommand(
            id,
            Actor.SubjectIdAsGuid.Value,
            request.DisplayName,
            request.AccessPolicy);

        var result = await sender.Send(command, ct);

        if (result == null)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to update asset" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an asset.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsset(
        Guid id,
        CancellationToken ct = default)
    {
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var command = new DeleteAssetCommand(id, Actor.SubjectIdAsGuid.Value);

        var result = await sender.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to delete asset" });
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
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var command = new ReportAssetCommand(
            id,
            Actor.SubjectIdAsGuid.Value,
            request.Reason,
            request.Description);

        var result = await sender.Send(command, ct);

        if (result == null)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to report asset" });
        }

        return Ok(result);
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
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetUserAssetsQuery(
            Actor.SubjectIdAsGuid.Value,
            Actor.TenantId,
            skip,
            take);

        var result = await sender.Send(query, ct);

        return Ok(result);
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
            Actor.SubjectIdAsGuid,
            Actor.TenantId);

        var result = await sender.Send(query, ct);

        return Ok(result);
    }
}

public record UpdateAssetRequest(
    string? DisplayName = null,
    AssetAccessPolicy? AccessPolicy = null);

public record ReportAssetRequest(
    ReportReason Reason,
    string? Description = null);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Assets.Controllers;

/// <summary>
/// Admin controller for asset moderation.
/// </summary>
[ApiController]
[Route("api/admin/assets")]
[Authorize(Policy = "RequireAdminRole")]
public class AssetsAdminController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : ControllerBase
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    /// <summary>
    /// Get moderation queue.
    /// </summary>
    [HttpGet("moderation-queue")]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var query = new GetModerationQueueQuery(limit);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    /// <summary>
    /// Get reports for an asset.
    /// </summary>
    [HttpGet("{id:guid}/reports")]
    public async Task<IActionResult> GetAssetReports(
        Guid id,
        CancellationToken ct = default)
    {
        var query = new GetAssetReportsQuery(id);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    /// <summary>
    /// Review a moderation report.
    /// </summary>
    [HttpPost("reports/{reportId:guid}/review")]
    public async Task<IActionResult> ReviewReport(
        Guid reportId,
        [FromBody] ReviewReportRequest request,
        CancellationToken ct = default)
    {
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var command = new ReviewReportCommand(
            reportId,
            Actor.SubjectIdAsGuid.Value,
            request.Decision,
            request.Notes);

        var result = await sender.Send(command, ct);

        if (result == null)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to review report" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Force delete an asset (admin override).
    /// </summary>
    [HttpDelete("{id:guid}/force")]
    public async Task<IActionResult> ForceDeleteAsset(
        Guid id,
        CancellationToken ct = default)
    {
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var command = new DeleteAssetCommand(id, Actor.SubjectIdAsGuid.Value, ForceDelete: true);

        var result = await sender.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to delete asset" });
        }

        return NoContent();
    }

    /// <summary>
    /// Get pending virus scans (for background processing).
    /// </summary>
    [HttpGet("pending-virus-scans")]
    public async Task<IActionResult> GetPendingVirusScans(
        [FromServices] IAssetContentRepository contentRepository,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var items = await contentRepository.GetPendingVirusScanAsync(limit, ct);

        return Ok(items.Select(x => new
        {
            x.Id,
            x.ContentHash,
            x.BucketName,
            x.ObjectKey,
            x.MimeType,
            x.SizeBytes,
            x.CreatedAt
        }));
    }

    /// <summary>
    /// Get pending moderation items (for background processing).
    /// </summary>
    [HttpGet("pending-moderation")]
    public async Task<IActionResult> GetPendingModeration(
        [FromServices] IAssetContentRepository contentRepository,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var items = await contentRepository.GetPendingModerationAsync(limit, ct);

        return Ok(items.Select(x => new
        {
            x.Id,
            x.ContentHash,
            x.BucketName,
            x.ObjectKey,
            x.MimeType,
            x.SizeBytes,
            x.ModerationStatus,
            x.CreatedAt
        }));
    }

    /// <summary>
    /// Get garbage collection candidates.
    /// </summary>
    [HttpGet("gc-candidates")]
    public async Task<IActionResult> GetGarbageCollectionCandidates(
        [FromServices] IAssetContentRepository contentRepository,
        [FromQuery] int gracePeriodHours = 24,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var items = await contentRepository.GetGarbageCollectionCandidatesAsync(
            TimeSpan.FromHours(gracePeriodHours),
            limit,
            ct);

        return Ok(items.Select(x => new
        {
            x.Id,
            x.ContentHash,
            x.BucketName,
            x.ObjectKey,
            x.SizeBytes,
            x.MarkedForDeletionAt
        }));
    }

    /// <summary>
    /// Update virus scan status.
    /// </summary>
    [HttpPost("{contentId:guid}/virus-scan")]
    public async Task<IActionResult> UpdateVirusScanStatus(
        Guid contentId,
        [FromBody] UpdateVirusScanRequest request,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        var content = await contentRepository.GetByIdAsync(contentId, ct);
        if (content == null)
        {
            return NotFound();
        }

        content.SetVirusScanStatus(request.Status, request.ScanResult);
        await contentRepository.UpdateAsync(content, ct);

        return Ok(new { content.Id, content.VirusScanStatus });
    }

    #region Garbage Collection & Maintenance

    /// <summary>
    /// Trigger manual garbage collection.
    /// </summary>
    /// <remarks>
    /// Runs the garbage collection process manually instead of waiting for the scheduled background job.
    /// Only deletes content that has been marked for deletion and past the grace period.
    /// </remarks>
    [HttpPost("gc/run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerGarbageCollection(
        [FromServices] IAssetContentRepository contentRepository,
        [FromServices] IAssetStorageService storageService,
        [FromServices] ITransformedAssetRepository transformedRepository,
        [FromQuery] int gracePeriodHours = 24,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var candidates = await contentRepository.GetGarbageCollectionCandidatesAsync(
            TimeSpan.FromHours(gracePeriodHours),
            limit,
            ct);

        var deleted = 0;
        var failed = 0;

        foreach (var content in candidates)
        {
            try
            {
                // Delete transformed versions first
                await transformedRepository.DeleteBySourceAsync(content.Id, ct);

                // Delete from storage
                await storageService.DeleteAsync(content.BucketName, content.ObjectKey, ct);

                // Delete record
                await contentRepository.DeleteAsync(content.Id, ct);

                deleted++;
            }
            catch
            {
                failed++;
            }
        }

        return Ok(new
        {
            candidatesFound = candidates.Count,
            deleted,
            failed
        });
    }

    /// <summary>
    /// Mark an asset content as non-deletable (legal hold).
    /// </summary>
    /// <remarks>
    /// Prevents the asset from being garbage collected, even if all references are deleted.
    /// Use for legal holds, compliance requirements, or audit preservation.
    /// </remarks>
    [HttpPost("{contentId:guid}/undeletable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsNonDeletable(
        Guid contentId,
        [FromBody] MarkNonDeletableRequest? request,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        var content = await contentRepository.GetByIdAsync(contentId, ct);
        if (content == null)
        {
            return NotFound();
        }

        content.MarkAsNonDeletable(request?.Reason);
        await contentRepository.UpdateAsync(content, ct);

        return Ok(new
        {
            content.Id,
            content.IsDeletable,
            Reason = request?.Reason
        });
    }

    /// <summary>
    /// Remove the non-deletable flag from an asset.
    /// </summary>
    [HttpDelete("{contentId:guid}/undeletable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveNonDeletable(
        Guid contentId,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        var content = await contentRepository.GetByIdAsync(contentId, ct);
        if (content == null)
        {
            return NotFound();
        }

        content.MarkAsDeletable();
        await contentRepository.UpdateAsync(content, ct);

        return Ok(new { content.Id, content.IsDeletable });
    }

    #endregion

    #region Content Moderation

    /// <summary>
    /// Review and moderate content directly.
    /// </summary>
    /// <remarks>
    /// Unlike report review which handles user reports, this endpoint allows
    /// direct moderation of content by admins for proactive moderation workflows.
    /// </remarks>
    [HttpPost("{contentId:guid}/moderation/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewContentModeration(
        Guid contentId,
        [FromBody] ContentModerationRequest request,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var content = await contentRepository.GetByIdAsync(contentId, ct);
        if (content == null)
        {
            return NotFound();
        }

        content.SetModerationStatus(request.Status, Actor.SubjectIdAsGuid.Value, request.Labels, request.Notes);
        await contentRepository.UpdateAsync(content, ct);

        return Ok(new
        {
            content.Id,
            content.ModerationStatus,
            ReviewedBy = Actor.SubjectIdAsGuid.Value,
            ReviewedAt = DateTime.UtcNow
        });
    }

    #endregion
}

public record ReviewReportRequest(
    ReviewDecision Decision,
    string? Notes = null);

public record UpdateVirusScanRequest(
    VirusScanStatus Status,
    string? ScanResult = null);

public record MarkNonDeletableRequest(
    string? Reason = null);

public record ContentModerationRequest(
    ModerationStatus Status,
    string[]? Labels = null,
    string? Notes = null);

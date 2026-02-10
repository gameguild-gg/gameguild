using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;

namespace GameGuild.Assets.Controllers;

/// <summary>
/// Admin controller for asset moderation.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/assets")]
[Authorize(Policy = "RequireAdminRole")]
public class AssetsAdminController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
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
        var result = await sender.Send(query, ct).ConfigureAwait(false);

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
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Review a moderation report.
    /// </summary>
    [HttpPost("reports/{reportId:guid}:review")]
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

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        if (result == null)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to review report" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Force delete an asset (admin override).
    /// </summary>
    [HttpPost("{id:guid}:force-delete")]
    public async Task<IActionResult> ForceDeleteAsset(
        Guid id,
        CancellationToken ct = default)
    {
        if (!Actor.SubjectIdAsGuid.HasValue)
        {
            return Unauthorized();
        }

        var command = new DeleteAssetCommand(id, Actor.SubjectIdAsGuid.Value, ForceDelete: true);

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails { Title = "Unable to delete asset" });
        }

        return NoContent();
    }

    /// <summary>
    /// List admin assets with optional status filter.
    /// Use status=pending-virus-scan or status=pending-moderation to filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAdminAssets(
        [FromServices] IAssetContentRepository contentRepository,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        if (status == "pending-virus-scan")
        {
            var virusScanItems = await contentRepository.GetPendingVirusScanAsync(limit, ct).ConfigureAwait(false);
            return Ok(virusScanItems.Select(x => new
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

        if (status == "pending-moderation")
        {
            var moderationItems = await contentRepository.GetPendingModerationAsync(limit, ct).ConfigureAwait(false);
            return Ok(moderationItems.Select(x => new
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

        // Default: return empty or all based on requirements
        return Ok(new { message = "Use status=pending-virus-scan or status=pending-moderation to filter" });
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
            ct).ConfigureAwait(false);

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
    /// Run virus scan on an asset.
    /// </summary>
    [HttpPost("{contentId:guid}:run-virus-scan")]
    public async Task<IActionResult> UpdateVirusScanStatus(
        Guid contentId,
        [FromBody] UpdateVirusScanRequest request,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        var content = await contentRepository.GetByIdAsync(contentId, ct).ConfigureAwait(false);
        if (content == null)
        {
            return NotFound();
        }

        content.SetVirusScanStatus(request.Status, request.ScanResult);
        await contentRepository.UpdateAsync(content, ct).ConfigureAwait(false);

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
    [HttpPost(":run-gc")]
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
            ct).ConfigureAwait(false);

        var deleted = 0;
        var failed = 0;

        foreach (var content in candidates)
        {
            try
            {
                // Delete transformed versions first
                await transformedRepository.DeleteBySourceAsync(content.Id, ct).ConfigureAwait(false);

                // Delete from storage
                await storageService.DeleteAsync(content.BucketName, content.ObjectKey, ct).ConfigureAwait(false);

                // Delete record
                await contentRepository.DeleteAsync(content.Id, ct).ConfigureAwait(false);

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
    [HttpPost("{contentId:guid}:mark-undeletable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsNonDeletable(
        Guid contentId,
        [FromBody] MarkNonDeletableRequest? request,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        var content = await contentRepository.GetByIdAsync(contentId, ct).ConfigureAwait(false);
        if (content == null)
        {
            return NotFound();
        }

        content.MarkAsNonDeletable(request?.Reason);
        await contentRepository.UpdateAsync(content, ct).ConfigureAwait(false);

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
    [HttpPost("{contentId:guid}:unmark-undeletable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveNonDeletable(
        Guid contentId,
        [FromServices] IAssetContentRepository contentRepository,
        CancellationToken ct = default)
    {
        var content = await contentRepository.GetByIdAsync(contentId, ct).ConfigureAwait(false);
        if (content == null)
        {
            return NotFound();
        }

        content.MarkAsDeletable();
        await contentRepository.UpdateAsync(content, ct).ConfigureAwait(false);

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
    [HttpPost("{contentId:guid}:review-moderation")]
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

        var content = await contentRepository.GetByIdAsync(contentId, ct).ConfigureAwait(false);
        if (content == null)
        {
            return NotFound();
        }

        content.SetModerationStatus(request.Status, Actor.SubjectIdAsGuid.Value, request.Labels, request.Notes);
        await contentRepository.UpdateAsync(content, ct).ConfigureAwait(false);

        return Ok(new
        {
            content.Id,
            content.ModerationStatus,
            ReviewedBy = Actor.SubjectIdAsGuid.Value,
            ReviewedAt = SystemClock.UtcNow
        });
    }

    #endregion
}

public sealed record ReviewReportRequest(
    ReviewDecision Decision,
    string? Notes = null);

public sealed record UpdateVirusScanRequest(
    VirusScanStatus Status,
    string? ScanResult = null);

public sealed record MarkNonDeletableRequest(
    string? Reason = null);

public sealed record ContentModerationRequest(
    ModerationStatus Status,
    string[]? Labels = null,
    string? Notes = null);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;
using GameGuild.Identity.Context;

namespace GameGuild.Assets.Controllers;

/// <summary>
/// Admin controller for asset moderation.
/// </summary>
[ApiController]
[Route("api/admin/assets")]
[Authorize(Policy = "RequireAdminRole")]
public class AssetsAdminController : ControllerBase
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IActorContext _actorContext;

    public AssetsAdminController(
        IRequestDispatcher dispatcher,
        IActorContext actorContext)
    {
        _dispatcher = dispatcher;
        _actorContext = actorContext;
    }

    /// <summary>
    /// Get moderation queue.
    /// </summary>
    [HttpGet("moderation-queue")]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var query = new GetModerationQueueQuery(limit);
        var result = await _dispatcher.DispatchAsync(query, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
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
        var result = await _dispatcher.DispatchAsync(query, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
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
        var command = new ReviewReportCommand(
            reportId,
            _actorContext.UserId,
            request.Decision,
            request.Notes);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Force delete an asset (admin override).
    /// </summary>
    [HttpDelete("{id:guid}/force")]
    public async Task<IActionResult> ForceDeleteAsset(
        Guid id,
        CancellationToken ct = default)
    {
        var command = new DeleteAssetCommand(id, _actorContext.UserId, ForceDelete: true);

        var result = await _dispatcher.DispatchAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Get pending virus scans (for background processing).
    /// </summary>
    [HttpGet("pending-virus-scans")]
    public async Task<IActionResult> GetPendingVirusScans(
        [FromQuery] int limit = 100,
        [FromServices] IAssetContentRepository contentRepository,
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
        [FromQuery] int limit = 100,
        [FromServices] IAssetContentRepository contentRepository,
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
        [FromQuery] int gracePeriodHours = 24,
        [FromQuery] int limit = 100,
        [FromServices] IAssetContentRepository contentRepository,
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
}

public record ReviewReportRequest(
    ReviewDecision Decision,
    string? Notes = null);

public record UpdateVirusScanRequest(
    VirusScanStatus Status,
    string? ScanResult = null);

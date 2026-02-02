using GameGuild.Identity.Context.Actors;
using GameGuild.Models;
using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources.Contents;

/// <summary>
/// Implementation of the content versioning service
/// </summary>
public class ContentVersioningService : IContentVersioningService
{
    private readonly IApplicationDbContext _db;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<ContentVersioningService> _logger;

    public ContentVersioningService(
        IApplicationDbContext db,
        IActorContextAccessor actorContextAccessor,
        ILogger<ContentVersioningService> logger)
    {
        _db = db;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;

    // ─── Draft Management ────────────────────────────────────────────────────────

    public async Task<Result<ContentVersion>> CreateDraftAsync(
        Guid entityId,
        string entityType,
        string title,
        Guid createdBy,
        string? summary = null,
        string? body = null,
        string? metadata = null,
        string? changeNotes = null,
        CancellationToken ct = default)
    {
        // Get next version number
        var maxVersion = await _db.Set<ContentVersion>()
            .Where(v => v.EntityId == entityId && v.EntityType == entityType && !v.IsDeleted)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;

        var version = ContentVersion.Create(
            entityId,
            entityType,
            maxVersion + 1,
            title,
            createdBy,
            summary,
            body,
            metadata,
            changeNotes);

        _db.Set<ContentVersion>().Add(version);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created draft version {VersionNumber} for {EntityType}:{EntityId}",
            version.VersionNumber, entityType, entityId);

        return Result.Success(version);
    }

    public async Task<Result<ContentVersion>> UpdateDraftAsync(
        Guid versionId,
        string? title = null,
        string? summary = null,
        string? body = null,
        string? metadata = null,
        string? changeNotes = null,
        CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        if (version.Status != ContentVersionStatus.Draft)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.CanOnlyUpdateDrafts);

        version.UpdateDraft(title, summary, body, metadata, changeNotes);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated draft version {VersionId}", versionId);

        return Result.Success(version);
    }

    public async Task<Result<ContentVersion>> GetDraftAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var draft = await _db.Set<ContentVersion>()
            .Where(v => v.EntityId == entityId && v.EntityType == entityType && !v.IsDeleted)
            .Where(v => v.Status == ContentVersionStatus.Draft)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (draft == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        return Result.Success(draft);
    }

    // ─── Review Workflow ─────────────────────────────────────────────────────────

    public async Task<Result<ContentVersion>> SubmitForReviewAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        try
        {
            version.SubmitForReview(GetCurrentUserId());
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Version {VersionId} submitted for review", versionId);
            return Result.Success(version);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ContentVersion>(Error.Failure("ContentVersioning.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ContentVersion>> ApproveAsync(Guid versionId, string? reviewNotes = null, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        try
        {
            version.Approve(GetCurrentUserId(), reviewNotes);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Version {VersionId} approved", versionId);
            return Result.Success(version);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ContentVersion>(Error.Failure("ContentVersioning.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ContentVersion>> RejectAsync(Guid versionId, string? reviewNotes = null, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        try
        {
            version.Reject(GetCurrentUserId(), reviewNotes);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Version {VersionId} rejected", versionId);
            return Result.Success(version);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ContentVersion>(Error.Failure("ContentVersioning.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<ContentVersion>>> GetPendingReviewAsync(
        string? entityType = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        var query = _db.Set<ContentVersion>()
            .Where(v => !v.IsDeleted)
            .Where(v => v.Status == ContentVersionStatus.PendingReview);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(v => v.EntityType == entityType);

        var versions = await query
            .OrderBy(v => v.SubmittedForReviewAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result.Success<IEnumerable<ContentVersion>>(versions);
    }

    public async Task<Result<ContentVersionReview>> AddReviewAsync(
        Guid versionId,
        ContentReviewDecision decision,
        string? feedback = null,
        string? suggestions = null,
        CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersionReview>(ContentVersioningErrors.NotFound);

        var review = ContentVersionReview.Create(
            versionId,
            GetCurrentUserId(),
            decision,
            feedback,
            suggestions);

        _db.Set<ContentVersionReview>().Add(review);
        await _db.SaveChangesAsync(ct);

        return Result.Success(review);
    }

    // ─── Publishing ──────────────────────────────────────────────────────────────

    public async Task<Result<ContentVersion>> PublishAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        try
        {
            // Unset current version flag on old versions
            var currentVersions = await _db.Set<ContentVersion>()
                .Where(v => v.EntityId == version.EntityId && v.EntityType == version.EntityType && !v.IsDeleted)
                .Where(v => v.IsCurrentVersion)
                .ToListAsync(ct);

            foreach (var cv in currentVersions)
                cv.SetAsCurrent(false);

            version.Publish(GetCurrentUserId());
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Version {VersionId} published for {EntityType}:{EntityId}",
                versionId, version.EntityType, version.EntityId);

            return Result.Success(version);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ContentVersion>(Error.Failure("ContentVersioning.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ContentVersion>> SchedulePublishAsync(Guid versionId, DateTime scheduledAt, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        if (scheduledAt <= DateTime.UtcNow)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.ScheduleDateMustBeFuture);

        try
        {
            version.SchedulePublish(scheduledAt, GetCurrentUserId());
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Version {VersionId} scheduled for publishing at {ScheduledAt}", versionId, scheduledAt);

            return Result.Success(version);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ContentVersion>(Error.Failure("ContentVersioning.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ContentVersion>> CancelScheduledPublishAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        if (version.Status != ContentVersionStatus.Scheduled)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotScheduled);

        // Revert to approved status
        version.Approve(GetCurrentUserId(), "Scheduled publishing cancelled");
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Scheduled publishing cancelled for version {VersionId}", versionId);

        return Result.Success(version);
    }

    public async Task<Result<int>> ProcessScheduledPublishingAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        
        var scheduledVersions = await _db.Set<ContentVersion>()
            .Where(v => !v.IsDeleted)
            .Where(v => v.Status == ContentVersionStatus.Scheduled)
            .Where(v => v.ScheduledPublishAt <= now)
            .ToListAsync(ct);

        var publishedCount = 0;

        foreach (var version in scheduledVersions)
        {
            try
            {
                // Unset current version flag on old versions
                var currentVersions = await _db.Set<ContentVersion>()
                    .Where(v => v.EntityId == version.EntityId && v.EntityType == version.EntityType && !v.IsDeleted)
                    .Where(v => v.IsCurrentVersion)
                    .ToListAsync(ct);

                foreach (var cv in currentVersions)
                    cv.SetAsCurrent(false);

                version.Publish(version.PublishedBy ?? Guid.Empty);
                publishedCount++;

                _logger.LogInformation("Auto-published scheduled version {VersionId} for {EntityType}:{EntityId}",
                    version.Id, version.EntityType, version.EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-publish version {VersionId}", version.Id);
            }
        }

        await _db.SaveChangesAsync(ct);

        return Result.Success(publishedCount);
    }

    // ─── Version History ─────────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<ContentVersion>>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        CancellationToken ct = default)
    {
        var versions = await _db.Set<ContentVersion>()
            .Where(v => v.EntityId == entityId && v.EntityType == entityType && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        return Result.Success<IEnumerable<ContentVersion>>(versions);
    }

    public async Task<Result<ContentVersion>> GetVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        return Result.Success(version);
    }

    public async Task<Result<ContentVersion>> GetVersionByNumberAsync(
        Guid entityId,
        string entityType,
        int versionNumber,
        CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v =>
                v.EntityId == entityId &&
                v.EntityType == entityType &&
                v.VersionNumber == versionNumber &&
                !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        return Result.Success(version);
    }

    public async Task<Result<ContentVersion>> GetCurrentVersionAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var version = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v =>
                v.EntityId == entityId &&
                v.EntityType == entityType &&
                v.IsCurrentVersion &&
                !v.IsDeleted, ct);

        if (version == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        return Result.Success(version);
    }

    public async Task<Result<ContentVersionDiff>> CompareVersionsAsync(
        Guid versionId1,
        Guid versionId2,
        CancellationToken ct = default)
    {
        var v1 = await _db.Set<ContentVersion>().FirstOrDefaultAsync(v => v.Id == versionId1 && !v.IsDeleted, ct);
        var v2 = await _db.Set<ContentVersion>().FirstOrDefaultAsync(v => v.Id == versionId2 && !v.IsDeleted, ct);

        if (v1 == null || v2 == null)
            return Result.Failure<ContentVersionDiff>(ContentVersioningErrors.NotFound);

        if (v1.EntityId != v2.EntityId || v1.EntityType != v2.EntityType)
            return Result.Failure<ContentVersionDiff>(ContentVersioningErrors.VersionsMustBeSameEntity);

        var diff = new ContentVersionDiff(
            versionId1,
            versionId2,
            v1.VersionNumber,
            v2.VersionNumber,
            v1.Title != v2.Title,
            v1.Summary != v2.Summary,
            v1.Body != v2.Body,
            v1.Metadata != v2.Metadata,
            v1.Title != v2.Title ? $"'{v1.Title}' -> '{v2.Title}'" : null,
            v1.Summary != v2.Summary ? $"Summary changed" : null,
            v1.Body != v2.Body ? "Body changed" : null
        );

        return Result.Success(diff);
    }

    // ─── Rollback ────────────────────────────────────────────────────────────────

    public async Task<Result<ContentVersion>> RollbackAsync(
        Guid entityId,
        string entityType,
        int targetVersionNumber,
        string? reason = null,
        CancellationToken ct = default)
    {
        var targetVersion = await _db.Set<ContentVersion>()
            .FirstOrDefaultAsync(v =>
                v.EntityId == entityId &&
                v.EntityType == entityType &&
                v.VersionNumber == targetVersionNumber &&
                !v.IsDeleted, ct);

        if (targetVersion == null)
            return Result.Failure<ContentVersion>(ContentVersioningErrors.NotFound);

        // Create new version based on target
        var rollbackResult = await CreateDraftAsync(
            entityId,
            entityType,
            targetVersion.Title,
            GetCurrentUserId(),
            targetVersion.Summary,
            targetVersion.Body,
            targetVersion.Metadata,
            $"Rollback to v{targetVersionNumber}" + (reason != null ? $": {reason}" : ""),
            ct);

        if (!rollbackResult.IsSuccess)
            return rollbackResult;

        _logger.LogInformation("Created rollback version from v{TargetVersion} for {EntityType}:{EntityId}",
            targetVersionNumber, entityType, entityId);

        return rollbackResult;
    }

    // ─── Cleanup ─────────────────────────────────────────────────────────────────

    public async Task<Result<int>> ArchiveOldVersionsAsync(
        Guid entityId,
        string entityType,
        int keepCount = 10,
        CancellationToken ct = default)
    {
        var versionsToArchive = await _db.Set<ContentVersion>()
            .Where(v => v.EntityId == entityId && v.EntityType == entityType && !v.IsDeleted)
            .Where(v => v.Status == ContentVersionStatus.Published || v.Status == ContentVersionStatus.Archived)
            .Where(v => !v.IsCurrentVersion)
            .OrderByDescending(v => v.VersionNumber)
            .Skip(keepCount)
            .ToListAsync(ct);

        foreach (var version in versionsToArchive)
            version.Archive();

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Archived {Count} old versions for {EntityType}:{EntityId}",
            versionsToArchive.Count, entityType, entityId);

        return Result.Success(versionsToArchive.Count);
    }
}

/// <summary>
/// Standard errors for the content versioning service
/// </summary>
public static class ContentVersioningErrors
{
    public static Error NotFound => Error.NotFound("ContentVersioning.NotFound", "Content version not found");
    public static Error CanOnlyUpdateDrafts => Error.Failure("ContentVersioning.CanOnlyUpdateDrafts", "Can only update draft versions");
    public static Error ScheduleDateMustBeFuture => Error.Failure("ContentVersioning.ScheduleDateMustBeFuture", "Scheduled date must be in the future");
    public static Error NotScheduled => Error.Failure("ContentVersioning.NotScheduled", "Version is not scheduled for publishing");
    public static Error VersionsMustBeSameEntity => Error.Failure("ContentVersioning.VersionsMustBeSameEntity", "Versions must belong to the same entity");
}

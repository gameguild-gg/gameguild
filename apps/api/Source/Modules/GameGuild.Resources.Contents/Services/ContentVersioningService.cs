namespace GameGuild.Resources.Contents;

/// <summary>
/// Thin facade that delegates to focused sub-services.
/// Preserves the <see cref="IContentVersioningService"/> contract for backward compatibility.
/// </summary>
public class ContentVersioningService(
    IContentDraftService draftService,
    IContentReviewPublishingService reviewPublishingService,
    IContentVersionQueryService versionQueryService) : IContentVersioningService
{
    // ─── Draft Management ────────────────────────────────────────────────────────

    public Task<Result<ContentVersion>> CreateDraftAsync(
        Guid entityId, string entityType, string title, Guid createdBy,
        string? summary = null, string? body = null, string? metadata = null,
        string? changeNotes = null, CancellationToken ct = default)
        => draftService.CreateDraftAsync(entityId, entityType, title, createdBy, summary, body, metadata, changeNotes, ct);

    public Task<Result<ContentVersion>> UpdateDraftAsync(
        Guid versionId, string? title = null, string? summary = null,
        string? body = null, string? metadata = null, string? changeNotes = null,
        CancellationToken ct = default)
        => draftService.UpdateDraftAsync(versionId, title, summary, body, metadata, changeNotes, ct);

    public Task<Result<ContentVersion>> GetDraftAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => draftService.GetDraftAsync(entityId, entityType, ct);

    // ─── Review Workflow ─────────────────────────────────────────────────────────

    public Task<Result<ContentVersion>> SubmitForReviewAsync(Guid versionId, CancellationToken ct = default)
        => reviewPublishingService.SubmitForReviewAsync(versionId, ct);

    public Task<Result<ContentVersion>> ApproveAsync(Guid versionId, string? reviewNotes = null, CancellationToken ct = default)
        => reviewPublishingService.ApproveAsync(versionId, reviewNotes, ct);

    public Task<Result<ContentVersion>> RejectAsync(Guid versionId, string? reviewNotes = null, CancellationToken ct = default)
        => reviewPublishingService.RejectAsync(versionId, reviewNotes, ct);

    public Task<Result<IEnumerable<ContentVersion>>> GetPendingReviewAsync(
        string? entityType = null, int skip = 0, int take = 20, CancellationToken ct = default)
        => reviewPublishingService.GetPendingReviewAsync(entityType, skip, take, ct);

    public Task<Result<ContentVersionReview>> AddReviewAsync(
        Guid versionId, ContentReviewDecision decision,
        string? feedback = null, string? suggestions = null, CancellationToken ct = default)
        => reviewPublishingService.AddReviewAsync(versionId, decision, feedback, suggestions, ct);

    // ─── Publishing ──────────────────────────────────────────────────────────────

    public Task<Result<ContentVersion>> PublishAsync(Guid versionId, CancellationToken ct = default)
        => reviewPublishingService.PublishAsync(versionId, ct);

    public Task<Result<ContentVersion>> SchedulePublishAsync(Guid versionId, DateTime scheduledAt, CancellationToken ct = default)
        => reviewPublishingService.SchedulePublishAsync(versionId, scheduledAt, ct);

    public Task<Result<ContentVersion>> CancelScheduledPublishAsync(Guid versionId, CancellationToken ct = default)
        => reviewPublishingService.CancelScheduledPublishAsync(versionId, ct);

    public Task<Result<int>> ProcessScheduledPublishingAsync(CancellationToken ct = default)
        => reviewPublishingService.ProcessScheduledPublishingAsync(ct);

    // ─── Version History ─────────────────────────────────────────────────────────

    public Task<Result<IEnumerable<ContentVersion>>> GetVersionHistoryAsync(
        Guid entityId, string entityType, CancellationToken ct = default)
        => versionQueryService.GetVersionHistoryAsync(entityId, entityType, ct);

    public Task<Result<ContentVersion>> GetVersionAsync(Guid versionId, CancellationToken ct = default)
        => versionQueryService.GetVersionAsync(versionId, ct);

    public Task<Result<ContentVersion>> GetVersionByNumberAsync(
        Guid entityId, string entityType, int versionNumber, CancellationToken ct = default)
        => versionQueryService.GetVersionByNumberAsync(entityId, entityType, versionNumber, ct);

    public Task<Result<ContentVersion>> GetCurrentVersionAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => versionQueryService.GetCurrentVersionAsync(entityId, entityType, ct);

    public Task<Result<ContentVersionDiff>> CompareVersionsAsync(
        Guid versionId1, Guid versionId2, CancellationToken ct = default)
        => versionQueryService.CompareVersionsAsync(versionId1, versionId2, ct);

    // ─── Rollback ────────────────────────────────────────────────────────────────

    public Task<Result<ContentVersion>> RollbackAsync(
        Guid entityId, string entityType, int targetVersionNumber,
        string? reason = null, CancellationToken ct = default)
        => draftService.RollbackAsync(entityId, entityType, targetVersionNumber, reason, ct);

    // ─── Cleanup ─────────────────────────────────────────────────────────────────

    public Task<Result<int>> ArchiveOldVersionsAsync(
        Guid entityId, string entityType, int keepCount = 10, CancellationToken ct = default)
        => draftService.ArchiveOldVersionsAsync(entityId, entityType, keepCount, ct);
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

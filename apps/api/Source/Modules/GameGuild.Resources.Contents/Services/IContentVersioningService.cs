
namespace GameGuild.Resources.Contents;

/// <summary>
/// Service interface for content versioning operations
/// </summary>
public interface IContentVersioningService
{
    // ─── Draft Management ────────────────────────────────────────────────────────

    /// <summary>Create a new draft version for an entity</summary>
    Task<Result<ContentVersion>> CreateDraftAsync(
        Guid entityId,
        string entityType,
        string title,
        Guid createdBy,
        string? summary = null,
        string? body = null,
        string? metadata = null,
        string? changeNotes = null,
        CancellationToken ct = default);

    /// <summary>Update an existing draft version</summary>
    Task<Result<ContentVersion>> UpdateDraftAsync(
        Guid versionId,
        string? title = null,
        string? summary = null,
        string? body = null,
        string? metadata = null,
        string? changeNotes = null,
        CancellationToken ct = default);

    /// <summary>Get the current draft for an entity (if exists)</summary>
    Task<Result<ContentVersion>> GetDraftAsync(Guid entityId, string entityType, CancellationToken ct = default);

    // ─── Review Workflow ─────────────────────────────────────────────────────────

    /// <summary>Submit a draft for review</summary>
    Task<Result<ContentVersion>> SubmitForReviewAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Approve a version</summary>
    Task<Result<ContentVersion>> ApproveAsync(Guid versionId, string? reviewNotes = null, CancellationToken ct = default);

    /// <summary>Reject a version</summary>
    Task<Result<ContentVersion>> RejectAsync(Guid versionId, string? reviewNotes = null, CancellationToken ct = default);

    /// <summary>Get versions pending review</summary>
    Task<Result<IEnumerable<ContentVersion>>> GetPendingReviewAsync(
        string? entityType = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    /// <summary>Add a review to a version</summary>
    Task<Result<ContentVersionReview>> AddReviewAsync(
        Guid versionId,
        ContentReviewDecision decision,
        string? feedback = null,
        string? suggestions = null,
        CancellationToken ct = default);

    // ─── Publishing ──────────────────────────────────────────────────────────────

    /// <summary>Publish an approved version</summary>
    Task<Result<ContentVersion>> PublishAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Schedule a version for future publishing</summary>
    Task<Result<ContentVersion>> SchedulePublishAsync(Guid versionId, DateTime scheduledAt, CancellationToken ct = default);

    /// <summary>Cancel scheduled publishing</summary>
    Task<Result<ContentVersion>> CancelScheduledPublishAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Process all scheduled versions that are ready to publish</summary>
    Task<Result<int>> ProcessScheduledPublishingAsync(CancellationToken ct = default);

    // ─── Version History ─────────────────────────────────────────────────────────

    /// <summary>Get all versions for an entity</summary>
    Task<Result<IEnumerable<ContentVersion>>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        CancellationToken ct = default);

    /// <summary>Get a specific version</summary>
    Task<Result<ContentVersion>> GetVersionAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Get a specific version by number</summary>
    Task<Result<ContentVersion>> GetVersionByNumberAsync(
        Guid entityId,
        string entityType,
        int versionNumber,
        CancellationToken ct = default);

    /// <summary>Get the current published version</summary>
    Task<Result<ContentVersion>> GetCurrentVersionAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Compare two versions</summary>
    Task<Result<ContentVersionDiff>> CompareVersionsAsync(
        Guid versionId1,
        Guid versionId2,
        CancellationToken ct = default);

    // ─── Rollback ────────────────────────────────────────────────────────────────

    /// <summary>Rollback to a previous version (creates new version based on old)</summary>
    Task<Result<ContentVersion>> RollbackAsync(
        Guid entityId,
        string entityType,
        int targetVersionNumber,
        string? reason = null,
        CancellationToken ct = default);

    // ─── Cleanup ─────────────────────────────────────────────────────────────────

    /// <summary>Archive old versions, keeping only the most recent N versions</summary>
    Task<Result<int>> ArchiveOldVersionsAsync(
        Guid entityId,
        string entityType,
        int keepCount = 10,
        CancellationToken ct = default);
}

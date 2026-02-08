namespace GameGuild.Resources.Contents;

/// <summary>
/// Service for review workflow and publishing operations.
/// </summary>
public interface IContentReviewPublishingService
{
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

    /// <summary>Publish an approved version</summary>
    Task<Result<ContentVersion>> PublishAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Schedule a version for future publishing</summary>
    Task<Result<ContentVersion>> SchedulePublishAsync(Guid versionId, DateTime scheduledAt, CancellationToken ct = default);

    /// <summary>Cancel scheduled publishing</summary>
    Task<Result<ContentVersion>> CancelScheduledPublishAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Process all scheduled versions that are ready to publish</summary>
    Task<Result<int>> ProcessScheduledPublishingAsync(CancellationToken ct = default);
}

namespace GameGuild.Assets;

/// <summary>
/// Service for content moderation.
/// </summary>
public interface IAssetModerationService
{
    /// <summary>
    /// Moderates content (virus scan, content analysis, etc.).
    /// </summary>
    Task<ModerationResult> ModerateAsync(
        Guid assetContentId,
        Stream content,
        string mimeType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets pending reports for review.
    /// </summary>
    Task<IReadOnlyList<AssetReport>> GetPendingReportsAsync(
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Submits a review for a report.
    /// </summary>
    Task<bool> SubmitReviewAsync(
        Guid reportId,
        Guid reviewerId,
        ReviewDecision decision,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new content report.
    /// </summary>
    Task<AssetReport?> CreateReportAsync(
        Guid assetReferenceId,
        Guid reportedByUserId,
        ReportReason reason,
        string? description = null,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a moderation operation.
/// </summary>
public record ModerationResult(
    bool IsApproved,
    ModerationStatus Status,
    double Confidence,
    string? DetectedIssue,
    string? Error);

namespace GameGuild.Assets;

/// <summary>
/// Service for content moderation.
/// </summary>
public interface IAssetModerationService
{
    /// <summary>
    /// Submits content for auto-moderation.
    /// </summary>
    Task<ModerationResult> SubmitForModerationAsync(Guid contentId, CancellationToken ct = default);

    /// <summary>
    /// Reviews and approves/rejects content.
    /// </summary>
    Task<ModerationResult> ReviewContentAsync(
        Guid contentId,
        Guid reviewerId,
        ModerationStatus status,
        IEnumerable<string>? labels = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the moderation queue.
    /// </summary>
    Task<IReadOnlyList<AssetContent>> GetModerationQueueAsync(
        int limit = 100,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a moderation operation.
/// </summary>
public record ModerationResult(
    Guid ContentId,
    ModerationStatus Status,
    IReadOnlyList<string> Labels,
    bool RequiresHumanReview);

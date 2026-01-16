namespace GameGuild.Assets.Moderation;

/// <summary>
/// Auto-moderation configuration options.
/// </summary>
public class AutoModerationOptions
{
    public const string SectionName = "Assets:AutoModeration";

    /// <summary>
    /// Whether auto-moderation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Confidence threshold for auto-approval (0.0-1.0).
    /// </summary>
    public double AutoApprovalThreshold { get; set; } = 0.95;

    /// <summary>
    /// Confidence threshold for auto-rejection (0.0-1.0).
    /// </summary>
    public double AutoRejectionThreshold { get; set; } = 0.9;

    /// <summary>
    /// MIME types that require human review regardless of auto-moderation score.
    /// </summary>
    public string[] RequireHumanReviewMimeTypes { get; set; } =
    [
        "image/gif",      // Animated GIFs can contain inappropriate content in frames
        "video/mp4",      // Video requires frame-by-frame analysis
        "video/webm"
    ];
}

/// <summary>
/// Auto-moderation result from ML analysis.
/// </summary>
public record AutoModerationResult(
    bool IsApproved,
    double Confidence,
    string[] DetectedLabels,
    string? RejectionReason = null);

/// <summary>
/// Queue item for moderation review.
/// </summary>
public record ModerationQueueItem(
    Guid AssetId,
    Guid ContentId,
    string MimeType,
    DateTime UploadedAt,
    double? AutoModerationScore,
    string[] DetectedLabels,
    int ReportCount);

// Note: The main IAssetModerationService and AssetModerationService 
// remain in GameGuild.Assets namespace for backward compatibility.
// This namespace contains auto-moderation extensions and queue management.

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Access review report
/// </summary>
public abstract class AccessReviewReport
{
    /// <summary>
    ///     Campaign ID
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    ///     Report format
    /// </summary>
    public AccessReviewReportFormat Format { get; set; }

    /// <summary>
    ///     Report content (JSON, CSV, or PDF bytes)
    /// </summary>
    public byte[ ] Content { get; set; } = [];

    /// <summary>
    ///     Content type for download
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    ///     Suggested filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     Report generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = SystemClock.UtcNow;
}

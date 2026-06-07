namespace GameGuild.Features;

/// <summary>
///     Analytics export result
/// </summary>
public sealed class AnalyticsExportResult
{
    /// <summary>
    ///     Export data content (formatted according to request format)
    /// </summary>
    public byte[ ] Content { get; set; } = [];

    /// <summary>
    ///     Content type (MIME type)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    ///     Suggested filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     Total records exported
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    ///     Export generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = SystemClock.UtcNow;
}

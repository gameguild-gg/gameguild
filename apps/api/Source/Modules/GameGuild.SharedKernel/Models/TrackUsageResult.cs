namespace GameGuild.Models;

/// <summary>
///     Result of usage tracking
/// </summary>
public record TrackUsageResult
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid? UsageTrackingId { get; init; }
}

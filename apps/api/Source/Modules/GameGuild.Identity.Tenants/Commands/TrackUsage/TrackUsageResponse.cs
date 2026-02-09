namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for usage tracking
/// </summary>
public sealed record TrackUsageResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid TrackingId { get; init; }
}

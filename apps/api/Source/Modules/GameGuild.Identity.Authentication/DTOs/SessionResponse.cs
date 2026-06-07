namespace GameGuild.Identity.Authentication;

/// <summary>
///     Session response
/// </summary>
public class SessionResponse
{
    public Guid Id { get; set; }

    public DeviceInfo? DeviceInfo { get; set; }

    public LocationInfo? Location { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime LastUsedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsTrustedDevice { get; set; }

    public bool IsCurrent { get; set; }
}

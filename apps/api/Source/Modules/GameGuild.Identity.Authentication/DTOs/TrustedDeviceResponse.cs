namespace GameGuild.Identity.Authentication;

/// <summary>
///     Trusted device response
/// </summary>
public class TrustedDeviceResponse
{
    public Guid Id { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public DeviceInfo? DeviceInfo { get; set; }

    public DateTime TrustedAt { get; set; }

    public DateTime LastUsedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

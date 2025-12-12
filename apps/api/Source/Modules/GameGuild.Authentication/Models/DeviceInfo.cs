namespace GameGuild.Authentication.Models;

/// <summary>
///     Represents detailed device information for security tracking.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    ///     Unique device fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    ///     Unique device identifier (derived from fingerprint or other sources).
    /// </summary>
    public string DeviceId { get => Fingerprint; }

    /// <summary>
    ///     IP address of the device making the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     User-friendly device name (e.g., "John's iPhone").
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    ///     Device type (Desktop, Mobile, Tablet, etc.).
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    ///     Operating system (Windows, macOS, iOS, Android, Linux).
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    ///     OS version.
    /// </summary>
    public string? OsVersion { get; set; }

    /// <summary>
    ///     Browser name (Chrome, Firefox, Safari, etc.).
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    ///     Browser version.
    /// </summary>
    public string? BrowserVersion { get; set; }

    /// <summary>
    ///     Screen resolution.
    /// </summary>
    public string? ScreenResolution { get; set; }

    /// <summary>
    ///     Device timezone.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    ///     Device language/locale.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    ///     Full user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Whether the device is a mobile device.
    /// </summary>
    public bool IsMobile { get; set; }

    /// <summary>
    ///     Whether the device is a bot or automated system.
    /// </summary>
    public bool IsBot { get; set; }
}

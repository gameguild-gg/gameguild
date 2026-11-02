namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to trust a device
/// </summary>
public class TrustDeviceRequest
{
    public string DeviceName { get; set; } = string.Empty;
}

/// <summary>
///     Request to name or rename one of the current user's own trusted devices
/// </summary>
public sealed record RenameTrustedDeviceRequest
{
    /// <summary>
    ///     User-defined trusted device name (1–200 characters, trimmed)
    /// </summary>
    public required string Name { get; init; }
}

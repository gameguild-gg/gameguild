namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to trust a device
/// </summary>
public class TrustDeviceRequest
{
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    ///     Fresh step-up receipt proving the caller just re-verified with an MFA factor for the
    ///     <c>trust-device</c> operation (issue #1144). Obtain it from the step-up challenge
    ///     flow bound to this device name.
    /// </summary>
    public string? StepUpReceipt { get; set; }
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

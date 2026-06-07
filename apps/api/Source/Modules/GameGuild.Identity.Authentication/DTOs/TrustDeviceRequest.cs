namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to trust a device
/// </summary>
public class TrustDeviceRequest
{
    public string DeviceName { get; set; } = string.Empty;
}

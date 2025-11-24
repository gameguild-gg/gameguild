namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Request to trust a device
/// </summary>
public class TrustDeviceRequest
{
    public string DeviceName { get; set; } = string.Empty;
}

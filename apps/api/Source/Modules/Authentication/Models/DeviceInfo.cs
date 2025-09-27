namespace GameGuild.Modules.Authentication;

/// <summary>
/// Device information parsed from user agent
/// </summary>
public class DeviceInfo
{
    public string Browser { get; set; } = string.Empty;

    public string Os { get; set; } = string.Empty;

    public string Device { get; set; } = string.Empty;
}
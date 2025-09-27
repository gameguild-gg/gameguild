namespace GameGuild.Modules.Authentication;

/// <summary>
/// Location information from IP address
/// </summary>
public class LocationInfo
{
    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;
}

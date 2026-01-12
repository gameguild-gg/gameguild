namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents geographic location information for security tracking.
/// </summary>
public class LocationInfo
{
    /// <summary>
    ///     IP address.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    ///     Country name.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    ///     ISO country code (US, GB, etc.).
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    ///     State or province.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    ///     City name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    ///     Postal/ZIP code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    ///     Geographic latitude coordinate.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    ///     Geographic longitude coordinate.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    ///     Timezone identifier (America/New_York, Europe/London, etc.).
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    ///     Internet Service Provider name.
    /// </summary>
    public string? Isp { get; set; }

    /// <summary>
    ///     Organization owning the IP range.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    ///     Whether the IP is from a known proxy/VPN.
    /// </summary>
    public bool? IsProxy { get; set; }

    /// <summary>
    ///     Whether the IP is from a known hosting provider.
    /// </summary>
    public bool? IsHosting { get; set; }

    /// <summary>
    ///     Gets a human-readable location string.
    /// </summary>
    public string DisplayLocation { get => !string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country) ? $"{City}, {Country}" : Country ?? "Unknown Location"; }
}

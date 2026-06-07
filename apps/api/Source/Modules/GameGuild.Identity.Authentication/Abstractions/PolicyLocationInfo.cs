namespace GameGuild.Identity.Authentication;

/// <summary>
///     Location information for policy evaluation
/// </summary>
public abstract class PolicyLocationInfo
{
    /// <summary>
    ///     Country code (ISO 3166-1 alpha-2)
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    ///     Region or state
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    ///     City name
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    ///     Latitude coordinate
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    ///     Longitude coordinate
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    ///     Timezone identifier
    /// </summary>
    public string? TimeZone { get; set; }
}

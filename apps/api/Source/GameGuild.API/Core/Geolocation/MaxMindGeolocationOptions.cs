namespace GameGuild.API.Geolocation;

/// <summary>
///     Host configuration for the offline MaxMind GeoLite2 adapter. Bound to
/// <c>Geolocation:MaxMind</c>. Absent/invalid configuration resolves to no location
/// (fail-closed to Unknown) rather than an error.
/// </summary>
public sealed class MaxMindGeolocationOptions
{
    /// <summary>Section name used by the composition root.</summary>
    public const string SectionName = "Geolocation:MaxMind";

    /// <summary>Path to the GeoLite2-City(.mmdb) database file. Empty or missing file disables enrichment.</summary>
    public string? DatabasePath { get; set; }

    /// <summary>
    ///     Optional path to a GeoIP2 Anonymous IP (.mmdb) database. When present (and readable),
    ///     every resolved address is additionally checked for VPN/proxy/Tor/hosting traits
    ///     (issue #278 VPN/proxy detection). An absent file leaves those traits unknown
    ///     (<c>null</c>) — it never disables the city enrichment above.
    /// </summary>
    public string? AnonymousDatabasePath { get; set; }
}

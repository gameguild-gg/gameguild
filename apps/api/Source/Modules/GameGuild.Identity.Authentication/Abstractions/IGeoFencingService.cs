namespace GameGuild.Identity.Authentication;

/// <summary>Outcome of a geo-fence evaluation for one sign-in.</summary>
public sealed record GeoFenceDecision
{
    /// <summary>The sign-in is not restricted by the geo-fencing policy.</summary>
    public static GeoFenceDecision Allowed { get; } = new() { IsBlocked = false };

    /// <summary>The resolved country matched the effective blocked set (or unknown-country failed closed).</summary>
    public bool IsBlocked { get; private init; }

    /// <summary>ISO 3166-1 alpha-2 code that matched, when known.</summary>
    public string? MatchedCountry { get; private init; }

    /// <summary>Whether the country could not be resolved at all.</summary>
    public bool CountryUnknown { get; private init; }

    public static GeoFenceDecision Blocked(string? matchedCountry, bool countryUnknown) => new()
    {
        IsBlocked = true,
        MatchedCountry = matchedCountry,
        CountryUnknown = countryUnknown
    };
}

/// <summary>
///     Evaluates the optional sign-in geo-fencing policy (issue #1087): resolves the client
///     IP's country through <see cref="IGeolocationProvider"/> and compares it with the
///     effective blocked-country set for the active tenant (platform-wide list plus
///     per-tenant additions). Implementations must never throw for unavailable geolocation
///     data — the unknown-country posture is a policy decision expressed by
///     <see cref="GeoFencingOptions.TreatUnknownCountryAsBlocked"/>.
/// </summary>
public interface IGeoFencingService
{
    /// <summary>
    ///     Evaluates the geo-fence for a sign-in. Returns <see cref="GeoFenceDecision.Allowed"/>
    ///     whenever geo-fencing is disabled by configuration.
    /// </summary>
    /// <param name="ipAddress">Client IP observed for the sign-in attempt.</param>
    /// <param name="tenantId">The resolved active tenant for the sign-in, when one is active.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GeoFenceDecision> EvaluateAsync(string? ipAddress, Guid? tenantId, CancellationToken cancellationToken = default);
}

namespace GameGuild.Identity.Authentication;

/// <summary>
///     How a geo-fence match influences a local password sign-in (issue #1087).
/// </summary>
public enum GeoFencingEnforcementMode
{
    /// <summary>Matches force the sign-in through the step-up (additional verification) flow.</summary>
    RequireStepUp = 0,

    /// <summary>Matches reject the sign-in outright; no tokens or session are issued.</summary>
    Block = 1
}

/// <summary>
///     Optional country/region geo-fencing for sign-in, bound from
///     <c>Authentication:GeoFencing</c> (issue #1087, GDPR and sanctions compliance for
///     enterprise tenants). Disabled by default: hosts opt in per deployment, and the
///     blocked-country set composes a platform-wide list with per-tenant additions,
///     mirroring <see cref="LocalSignInPolicyOptions"/>.
/// </summary>
public sealed class GeoFencingOptions
{
    public const string SectionName = "Authentication:GeoFencing";

    /// <summary>Master switch. Defaults to <c>false</c>: no geo-fencing until configured.</summary>
    public bool Enabled { get; set; }

    /// <summary>Enforcement for a match. Defaults to blocking the sign-in.</summary>
    public GeoFencingEnforcementMode Mode { get; set; } = GeoFencingEnforcementMode.Block;

    /// <summary>
    ///     Platform-wide blocked countries as ISO 3166-1 alpha-2 codes (US, GB, ...).
    ///     Compared case-insensitively after trimming.
    /// </summary>
    public List<string> BlockedCountries { get; set; } = [];

    /// <summary>
    ///     Per-tenant blocked-country additions keyed by tenant ID, for enterprise tenants
    ///     with their own sanctions posture. Composed with (never replacing) the global list.
    /// </summary>
    public Dictionary<string, List<string>> TenantBlockedCountries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     How to treat an IP whose country cannot be resolved (private address, unknown
    ///     prefix, geolocation provider unavailable). Defaults to allowing the sign-in so a
    ///     geolocation outage cannot lock every user out; operators in strict jurisdictions
    ///     set this to <c>true</c> to fail closed.
    /// </summary>
    public bool TreatUnknownCountryAsBlocked { get; set; }

    /// <summary>Validates the configured country codes and tenant keys.</summary>
    public bool IsValid() =>
        (BlockedCountries is null || BlockedCountries.All(static code => IsValidCountryCode(code)))
        && (TenantBlockedCountries is null || TenantBlockedCountries.Keys.All(static key => Guid.TryParse(key, out _))
            && TenantBlockedCountries.Values.All(static codes => codes is null || codes.All(IsValidCountryCode)))
        && Enum.IsDefined(Mode);

    internal static bool IsValidCountryCode(string? code) =>
        !string.IsNullOrWhiteSpace(code) && code.Trim().Length == 2;
}

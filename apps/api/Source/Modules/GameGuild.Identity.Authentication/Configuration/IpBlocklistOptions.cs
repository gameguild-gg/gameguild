namespace GameGuild.Identity.Authentication;

/// <summary>
///     How a proactive IP blocklist match (issue #1089) influences a local password sign-in.
/// </summary>
public enum IpBlocklistEnforcementMode
{
    /// <summary>Matches force the sign-in through the step-up (additional verification) flow.</summary>
    RequireStepUp = 0,

    /// <summary>Matches reject the sign-in outright; no tokens or session are issued.</summary>
    Block = 1
}

/// <summary>
///     Proactive blocklist of known-attacker IP addresses (issue #1089), bound from
///     <c>Authentication:IpBlocklist</c>. Entries live in the distributed cache (Redis in
/// production, so the list is shared by every API instance) and are managed through the
/// admin endpoint. With an empty list the blocklist is inert.
/// </summary>
public sealed class IpBlocklistOptions
{
    public const string SectionName = "Authentication:IpBlocklist";

    /// <summary>Master switch. Defaults to enabled: the shipped empty list blocks nothing.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Enforcement for a match. Defaults to blocking; step-up keeps the account usable
    ///     from the listed IP only after additional verification.
    /// </summary>
    public IpBlocklistEnforcementMode Mode { get; set; } = IpBlocklistEnforcementMode.Block;

    /// <summary>
    ///     Default entry lifetime applied when an admin adds an entry without an explicit
    ///     expiry, so stale entries age out instead of accumulating forever.
    /// </summary>
    public TimeSpan DefaultEntryLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Validates the configured values.</summary>
    public bool IsValid() => DefaultEntryLifetime > TimeSpan.Zero && Enum.IsDefined(Mode);
}

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Optional password age / rotation policy, bound from
///     <c>Authentication:PasswordAge</c> (issue #1088). Disabled by default; enterprise
///     tenants typically enable it with 90 days. A sign-in with a password older than the
///     effective maximum is rejected with <c>PasswordExpired</c> and the owner completes the
///     existing password-reset flow to rotate it. Reuse of recent passwords is already
///     blocked by <see cref="PasswordHistoryPolicy"/> on every change and reset.
/// </summary>
public sealed class PasswordAgePolicyOptions
{
    public const string SectionName = "Authentication:PasswordAge";

    /// <summary>Master switch. Defaults to <c>false</c>: passwords never expire.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Platform-wide maximum password age in days. Defaults to the enterprise baseline
    ///     of 90 days; only meaningful while <see cref="Enabled"/> is set.
    /// </summary>
    public int MaxAgeDays { get; set; } = 90;

    /// <summary>
    ///     Per-tenant overrides keyed by tenant ID (days). A tenant entry replaces the
    ///     platform-wide maximum for that tenant's members; there is deliberately no way to
    ///     weaken a stricter tenant from the global value because the global value is the
    ///     floor: effective age = tenant value when present, otherwise <see cref="MaxAgeDays"/>.
    /// </summary>
    public Dictionary<string, int> TenantMaxAgeDays { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Validates the configured values.</summary>
    public bool IsValid() =>
        MaxAgeDays > 0
        && (TenantMaxAgeDays is null
            || (TenantMaxAgeDays.Keys.All(static key => Guid.TryParse(key, out _))
                && TenantMaxAgeDays.Values.All(static days => days > 0)));
}

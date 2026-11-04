namespace GameGuild.Identity.Authentication;

/// <summary>
///     Per-IP rate limit for passwordless sign-in delivery requests (magic links and
///     one-time email codes), bound from <c>Authentication:PasswordlessRequests:IpRateLimit</c>
///     (issue #1090). The existing per-email throttles cannot stop email bombing across
///     many accounts from a single address, so both request endpoints additionally share
///     one fixed-window budget per client IP.
/// </summary>
public sealed class PasswordlessRequestIpRateLimitOptions
{
    public const string SectionName = "Authentication:PasswordlessRequests:IpRateLimit";

    /// <summary>Master switch. Defaults to enabled: the limit is deliberately generous.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Maximum number of magic-link and email-code requests a single IP may issue
    ///     within <see cref="Window"/>. The budget is shared between both flows because
    ///     they are the same outbound-email abuse channel.
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 20;

    /// <summary>Fixed window length for the per-IP budget. Defaults to one hour.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Validates the configured values.</summary>
    public bool IsValid() => MaxRequestsPerWindow > 0 && Window > TimeSpan.Zero;
}

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Digest engine tuning. Bound to configuration section "Notifications:DigestDispatcher".
/// </summary>
public sealed class DigestDispatcherOptions
{
    /// <summary>Delay between digest evaluation passes.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>User-local time of day each digest window fires (daily/weekly/biweekly).</summary>
    public TimeOnly FireTime { get; set; } = new(8, 0);
}

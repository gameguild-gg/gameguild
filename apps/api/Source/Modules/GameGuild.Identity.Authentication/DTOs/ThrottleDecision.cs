namespace GameGuild.Identity.Authentication;

/// <summary>
///     Decision on whether to throttle an authentication attempt
/// </summary>
public class ThrottleDecision
{
    public bool ShouldThrottle { get; set; }

    public string? Reason { get; set; }

    public DateTime? ThrottleUntil { get; set; }

    public int RemainingAttempts { get; set; }

    public int DelayMs { get; set; }

    public int AttemptCount { get; set; }

    public int TimeWindowMinutes { get; set; }
}

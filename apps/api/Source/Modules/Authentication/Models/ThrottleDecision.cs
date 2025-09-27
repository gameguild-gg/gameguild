namespace GameGuild.Modules.Authentication;

/// <summary>
/// Decision about whether to throttle an authentication attempt
/// </summary>
public class ThrottleDecision
{
    public bool ShouldThrottle { get; set; }

    public string? Reason { get; set; }

    public DateTime? ThrottleUntil { get; set; }

    public int RemainingAttempts { get; set; }
}

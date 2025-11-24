namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Decision on whether to throttle an authentication attempt
/// </summary>
public class ThrottleDecision
{
    public bool ShouldThrottle { get; set; }

    public string? Reason { get; set; }

    public DateTime? ThrottleUntil { get; set; }

    public int RemainingAttempts { get; set; }
}

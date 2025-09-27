namespace GameGuild.Modules.Authentication;

/// <summary>
/// Suspicious activity summary for monitoring
/// </summary>
public class SuspiciousActivity
{
    public string IpAddress { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public DateTime FirstAttempt { get; set; }

    public DateTime LastAttempt { get; set; }

    public int MaxRiskScore { get; set; }

    public int UniqueUserAgents { get; set; }

    public int SuccessfulAttempts { get; set; }
}

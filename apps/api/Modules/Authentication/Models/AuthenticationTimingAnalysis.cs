namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Result of timing analysis for monitoring
/// </summary>
public class AuthenticationTimingAnalysis
{
    public string EmailHash { get; set; } = string.Empty;

    public bool UserExists { get; set; }

    public TimeSpan ActualProcessingTime { get; set; }

    public TimeSpan TargetProcessingTime { get; set; }

    public TimeSpan TimingDeviation { get; set; }

    public DateTime Timestamp { get; set; }

    public string IpAddress { get; set; } = string.Empty;
}

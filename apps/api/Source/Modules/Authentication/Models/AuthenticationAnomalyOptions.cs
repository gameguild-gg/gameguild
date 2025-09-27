namespace GameGuild.Modules.Authentication.Configuration;

/// <summary>
/// Configuration options for authentication anomaly detection
/// </summary>
public class AuthenticationAnomalyOptions
{
    public const string SectionName = "Authentication:Anomaly";

    /// <summary>
    /// Whether anomaly detection is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum failed login attempts per hour for a single email
    /// </summary>
    public int MaxFailedAttemptsPerHour { get; set; } = 5;

    /// <summary>
    /// Maximum failed login attempts per day for a single email
    /// </summary>
    public int MaxFailedAttemptsPerDay { get; set; } = 20;

    /// <summary>
    /// Maximum total attempts per IP address per hour
    /// </summary>
    public int MaxAttemptsPerIpPerHour { get; set; } = 50;

    /// <summary>
    /// Risk score threshold above which an attempt is considered suspicious
    /// </summary>
    public int SuspiciousThreshold { get; set; } = 30;

    /// <summary>
    /// Duration in minutes to throttle after suspicious activity
    /// </summary>
    public int ThrottleMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to log detailed timing analysis
    /// </summary>
    public bool LogTimingAnalysis { get; set; } = false;

    /// <summary>
    /// Whether to automatically block IPs with high risk scores
    /// </summary>
    public bool AutoBlockSuspiciousIps { get; set; } = false;

    /// <summary>
    /// Risk score threshold for automatic IP blocking
    /// </summary>
    public int AutoBlockThreshold { get; set; } = 80;

    /// <summary>
    /// Duration in hours to automatically block suspicious IPs
    /// </summary>
    public int AutoBlockDurationHours { get; set; } = 24;
}
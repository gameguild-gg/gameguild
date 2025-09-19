namespace GameGuild.Modules.Authentication.Configuration;

/// <summary>
/// Configuration options for authentication anomaly detection
/// </summary>
public class AuthenticationAnomalyOptions {
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

/// <summary>
/// Configuration options for user enumeration protection
/// </summary>
public class UserEnumerationProtectionOptions {
    public const string SectionName = "Authentication:UserEnumerationProtection";

    /// <summary>
    /// Whether user enumeration protection is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum processing time in milliseconds
    /// </summary>
    public int MinProcessingTimeMs { get; set; } = 200;

    /// <summary>
    /// Maximum processing time in milliseconds
    /// </summary>
    public int MaxProcessingTimeMs { get; set; } = 800;

    /// <summary>
    /// Target processing time in milliseconds
    /// </summary>
    public int TargetProcessingTimeMs { get; set; } = 400;

    /// <summary>
    /// Whether to log timing information for analysis
    /// </summary>
    public bool LogTimingAnalysis { get; set; } = false;

    /// <summary>
    /// Custom error message to use (if not set, uses default)
    /// </summary>
    public string? CustomErrorMessage { get; set; }

    /// <summary>
    /// Whether to perform dummy password hashing for non-existent users
    /// </summary>
    public bool PerformDummyHashing { get; set; } = true;

    /// <summary>
    /// Additional delay variance in milliseconds to add randomness
    /// </summary>
    public int DelayVarianceMs { get; set; } = 100;
}

/// <summary>
/// Configuration options for authentication security features
/// </summary>
public class AuthenticationSecurityOptions {
    public const string SectionName = "Authentication:Security";

    /// <summary>
    /// Whether enhanced security features are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Anomaly detection settings
    /// </summary>
    public AuthenticationAnomalyOptions Anomaly { get; set; } = new();

    /// <summary>
    /// User enumeration protection settings
    /// </summary>
    public UserEnumerationProtectionOptions UserEnumerationProtection { get; set; } = new();

    /// <summary>
    /// Whether to require HTTPS for authentication endpoints
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Whether to enforce strong password requirements
    /// </summary>
    public bool EnforceStrongPasswords { get; set; } = true;

    /// <summary>
    /// Whether to require email verification for new accounts
    /// </summary>
    public bool RequireEmailVerification { get; set; } = true;

    /// <summary>
    /// Whether to log all authentication events
    /// </summary>
    public bool LogAllAuthEvents { get; set; } = true;

    /// <summary>
    /// Whether to use BCrypt for password hashing (vs legacy SHA256)
    /// </summary>
    public bool UseBCryptHashing { get; set; } = true;

    /// <summary>
    /// BCrypt work factor (rounds) for password hashing
    /// </summary>
    public int BCryptWorkFactor { get; set; } = 12;
}
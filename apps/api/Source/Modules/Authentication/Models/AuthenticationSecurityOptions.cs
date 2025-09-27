namespace GameGuild.Modules.Authentication.Configuration;

/// <summary>
/// Configuration options for authentication security features
/// </summary>
public class AuthenticationSecurityOptions
{
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

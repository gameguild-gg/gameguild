namespace GameGuild.Authentication.Models.Configuration;

/// <summary>
///     Tenant-specific authentication configuration and policies.
/// </summary>
public abstract class TenantAuthConfiguration
{
    /// <summary>
    ///     Tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Whether MFA is required for all users in this tenant.
    /// </summary>
    public bool RequireMfa { get; set; }

    /// <summary>
    ///     Whether email verification is required.
    /// </summary>
    public bool RequireEmailVerification { get; set; }

    /// <summary>
    ///     Allowed authentication methods for this tenant.
    /// </summary>
    public List<string> AllowedAuthMethods { get; set; } = new List<string>();

    /// <summary>
    ///     Allowed social providers for this tenant.
    /// </summary>
    public List<string> AllowedSocialProviders { get; set; } = new List<string>();

    /// <summary>
    ///     Whether Web3/blockchain authentication is enabled.
    /// </summary>
    public bool AllowWeb3Auth { get; set; }

    /// <summary>
    ///     Password policy configuration.
    /// </summary>
    public PasswordPolicy? PasswordPolicy { get; set; }

    /// <summary>
    ///     Session configuration.
    /// </summary>
    public SessionPolicy? SessionPolicy { get; set; }

    /// <summary>
    ///     Maximum number of failed login attempts before lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    ///     Account lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    ///     Whether to enable anomaly detection.
    /// </summary>
    public bool EnableAnomalyDetection { get; set; } = true;

    /// <summary>
    ///     Risk threshold for requiring additional authentication (0-100).
    /// </summary>
    public double RiskThreshold { get; set; } = 60;

    /// <summary>
    ///     Additional custom configuration settings.
    /// </summary>
    public Dictionary<string, object>? CustomSettings { get; set; }
}

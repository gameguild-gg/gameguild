using System.ComponentModel.DataAnnotations;
using GameGuild;

namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// Tracks authentication attempts for anomaly detection and security monitoring
/// </summary>
public class LoginAttempt : EntityBase {

    /// <summary>
    /// Email address used in the login attempt (normalized)
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User ID if the user exists (null for non-existent users to prevent enumeration)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// IP address of the login attempt
    /// </summary>
    [Required]
    [MaxLength(45)] // IPv6 max length
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent string from the request
    /// </summary>
    [MaxLength(1000)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the login attempt was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Type of failure if unsuccessful
    /// </summary>
    [MaxLength(50)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Timestamp of the attempt
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// Time taken to process the authentication (for timing analysis)
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Geographic location derived from IP (city, country)
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Device fingerprint hash for device tracking
    /// </summary>
    [MaxLength(64)]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// Session ID if available
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Tenant ID if provided in the request
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Whether this attempt was flagged as suspicious
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Risk score assigned to this attempt (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Additional metadata about the attempt
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    [MaxLength(64)]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Common failure reasons for login attempts
/// </summary>
public static class LoginFailureReasons {
    public const string InvalidCredentials = "InvalidCredentials";
    public const string UserNotFound = "UserNotFound";
    public const string AccountLocked = "AccountLocked";
    public const string MfaRequired = "MfaRequired";
    public const string MfaFailed = "MfaFailed";
    public const string RateLimited = "RateLimited";
    public const string SuspiciousActivity = "SuspiciousActivity";
    public const string AccountDisabled = "AccountDisabled";
    public const string PasswordExpired = "PasswordExpired";
    public const string TenantAccess = "TenantAccess";
    public const string ValidationError = "ValidationError";
}

/// <summary>
/// DTO for creating login attempt records
/// </summary>
public class CreateLoginAttemptRequest {
    public string Email { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? Location { get; set; }
    public string? DeviceFingerprint { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsSuspicious { get; set; }
    public int RiskScore { get; set; }
    public string? Metadata { get; set; }
    public string? CorrelationId { get; set; }
}
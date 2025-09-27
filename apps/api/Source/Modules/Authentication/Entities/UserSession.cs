namespace GameGuild.Modules.Authentication;

/// <summary>
/// User session with device information for session management
/// </summary>
public class UserSession : EntityBase
{
    public Guid UserId { get; set; }

    /// <summary>
    /// JWT refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token (for tracking, not storage)
    /// </summary>
    public string? AccessTokenHash { get; set; }

    /// <summary>
    /// IP address when session was created
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent string
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Device fingerprint for identification
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// Parsed device information
    /// </summary>
    public string? DeviceInfo { get; set; } // JSON: { os, browser, device }

    /// <summary>
    /// Geographic location (approximate)
    /// </summary>
    public string? Location { get; set; } // JSON: { city, country, region }

    /// <summary>
    /// Session expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Last time this session was used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// Whether session is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason for session termination
    /// </summary>
    public string? TerminationReason { get; set; }

    /// <summary>
    /// When session was terminated
    /// </summary>
    public DateTime? TerminatedAt { get; set; }

    /// <summary>
    /// Whether this is a trusted device
    /// </summary>
    public bool IsTrustedDevice { get; set; }

    /// <summary>
    /// When device was marked as trusted
    /// </summary>
    public DateTime? TrustedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsValid => IsActive && !IsExpired;
}

namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// Represents a user's MFA (Multi-Factor Authentication) configuration
/// </summary>
public class UserMfaConfiguration : EntityBase {
    public Guid UserId { get; set; }

    /// <summary>
    /// Indicates if MFA is enabled for this user
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// TOTP secret key (encrypted)
    /// </summary>
    public string? TotpSecretKey { get; set; }

    /// <summary>
    /// Backup codes for MFA recovery (encrypted, JSON array)
    /// </summary>
    public string? BackupCodes { get; set; }

    /// <summary>
    /// When MFA was first enabled
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// Last time MFA was used successfully
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Number of failed MFA attempts (reset on success)
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// When the user was locked out due to failed attempts
    /// </summary>
    public DateTime? LockedOutUntil { get; set; }

    /// <summary>
    /// Preferred MFA method
    /// </summary>
    public MfaMethod PreferredMethod { get; set; } = MfaMethod.Totp;

    /// <summary>
    /// QR code data for TOTP setup (temporary, cleared after setup)
    /// </summary>
    public string? QrCodeSetupData { get; set; }

    /// <summary>
    /// Indicates if the user has completed MFA setup
    /// </summary>
    public bool IsSetupComplete { get; set; }
}

/// <summary>
/// User session with device information for session management
/// </summary>
public class UserSession : EntityBase {
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

/// <summary>
/// MFA attempt log for security monitoring
/// </summary>
public class MfaAttempt : EntityBase {
    public Guid UserId { get; set; }

    /// <summary>
    /// MFA method used
    /// </summary>
    public MfaMethod Method { get; set; }

    /// <summary>
    /// Whether the attempt was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// IP address of the attempt
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent string
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Failure reason if unsuccessful
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Geographic location of attempt
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Session ID if successful
    /// </summary>
    public Guid? SessionId { get; set; }
}

/// <summary>
/// Device trust record for managing trusted devices
/// </summary>
public class TrustedDevice : EntityBase {
    public Guid UserId { get; set; }

    /// <summary>
    /// Device fingerprint
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name for the device
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device information
    /// </summary>
    public string DeviceInfo { get; set; } = string.Empty; // JSON

    /// <summary>
    /// When device was trusted
    /// </summary>
    public DateTime TrustedAt { get; set; }

    /// <summary>
    /// Last time device was used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// Whether trust is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When trust expires (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// IP addresses associated with this device
    /// </summary>
    public string? AssociatedIpAddresses { get; set; } // JSON array

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;
    public bool IsValid => IsActive && !IsExpired;
}

/// <summary>
/// Supported MFA methods
/// </summary>
public enum MfaMethod {
    Totp = 1,
    BackupCode = 2,
    // Future: SMS = 3, Email = 4, WebAuthn = 5
}

/// <summary>
/// Session termination reasons
/// </summary>
public enum SessionTerminationReason {
    UserLogout,
    AdminTermination,
    Expired,
    SecurityViolation,
    DeviceChanged,
    LocationChanged,
    MaxSessionsExceeded
}

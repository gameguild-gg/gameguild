namespace GameGuild.Modules.Authentication;

/// <summary>
/// Request for phone number authentication
/// </summary>
public class PhoneAuthenticationRequest
{
    /// <summary>
    /// Phone number in international format (e.g., +1234567890)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant context
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Optional device fingerprint
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}

/// <summary>
/// Request for phone number verification via SMS
/// </summary>
public class PhoneVerificationRequest
{
    /// <summary>
    /// Phone number to verify
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Verification code sent via SMS
    /// </summary>
    public string VerificationCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant context
    /// </summary>
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Request to send phone verification code
/// </summary>
public class SendPhoneVerificationRequest
{
    /// <summary>
    /// Phone number to send verification code to
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional user ID if already registered
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Optional tenant context
    /// </summary>
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Response for phone verification operations
/// </summary>
public class PhoneVerificationResponse
{
    /// <summary>
    /// Whether verification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Verification code (only in development mode)
    /// </summary>
    public string? VerificationCode { get; set; }

    /// <summary>
    /// Time when code expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message if verification failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request for username-based authentication
/// </summary>
public class UsernameAuthenticationRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant context
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Optional device fingerprint
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}

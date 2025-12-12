using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Entities;

/// <summary>
///     Represents an MFA verification attempt.
///     Tracks each MFA challenge and its outcome for security monitoring.
/// </summary>
public class MfaAttempt
{
    /// <summary>
    ///     Unique identifier for the MFA attempt.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The user attempting MFA verification.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     The MFA method used for this attempt.
    /// </summary>
    public MfaMethod Method { get; set; }

    /// <summary>
    ///     Whether the MFA attempt was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    ///     Reason for failure if attempt was unsuccessful.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    ///     IP address from which the MFA attempt was made.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    ///     User agent string of the client.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    ///     When the MFA attempt occurred.
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    ///     How long the verification took to process (in milliseconds).
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    ///     Device fingerprint associated with the attempt.
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    ///     Associated authentication session ID.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    ///     Tenant context if applicable.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Additional metadata about the attempt (JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    ///     When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

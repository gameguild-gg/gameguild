namespace GameGuild.Modules.Authentication;

/// <summary>
/// MFA attempt log for security monitoring
/// </summary>
public class MfaAttempt : EntityBase
{
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

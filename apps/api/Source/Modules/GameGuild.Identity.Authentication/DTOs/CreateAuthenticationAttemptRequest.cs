namespace GameGuild.Identity.Authentication;

/// <summary>
///     DTO for creating authentication attempt records
/// </summary>
public abstract class CreateAuthenticationAttemptRequest
{
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

    public string? SuspicionReason { get; set; }

    public string? Metadata { get; set; }

    public string? CorrelationId { get; set; }
}

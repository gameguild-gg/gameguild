namespace GameGuild.Identity.Authentication;

/// <summary>
///     Response containing authentication attempt details
/// </summary>
public class AuthenticationAttemptResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public DateTime AttemptedAt { get; set; }

    public bool IsSuspicious { get; set; }
}

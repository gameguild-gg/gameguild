using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to verify a user's email address using a verification token
/// </summary>
public class VerifyEmailCommand : ICommand<EmailVerificationResult>
{
    /// <summary>
    ///     The verification token sent to the user's email
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    ///     Optional tenant context
    /// </summary>
    public Guid? TenantId { get; init; }
}

/// <summary>
///     Result of email verification
/// </summary>
public class EmailVerificationResult
{
    /// <summary>
    ///     Whether verification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     The email address that was verified
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     User ID whose email was verified
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     When the verification was completed
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
}

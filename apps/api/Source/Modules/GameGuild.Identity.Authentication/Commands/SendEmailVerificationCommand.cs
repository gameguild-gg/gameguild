using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to send an email verification link to the specified email address.
///     Coordinates IEmailVerificationService to generate a token and dispatch the verification email.
/// </summary>
public sealed record SendEmailVerificationCommand : ICommand<EmailVerificationResponse>
{
    /// <summary>
    ///     The email address to send verification to.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    ///     Optional user ID (when the user is known/authenticated).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Optional user name for personalization.
    /// </summary>
    public string? UserName { get; init; }
}

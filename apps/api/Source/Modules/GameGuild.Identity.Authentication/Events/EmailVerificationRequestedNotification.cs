using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Notification raised when a verification email should be delivered.
/// </summary>
public sealed class EmailVerificationRequestedNotification : INotification
{
    public required string Email { get; init; }

    public required string Token { get; init; }

    public string? UserName { get; init; }
}

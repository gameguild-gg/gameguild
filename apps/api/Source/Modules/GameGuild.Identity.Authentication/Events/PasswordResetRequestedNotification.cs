using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Notification raised when a password reset email should be delivered.
/// </summary>
public sealed class PasswordResetRequestedNotification : INotification
{
    public required string Email { get; init; }

    public required string Token { get; init; }

    public string? UserName { get; init; }
}

using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Notification raised when a one-time email-MFA code should be delivered (issue #1152).
/// </summary>
public sealed class EmailMfaCodeRequestedNotification : INotification
{
    public required Guid UserId { get; init; }

    /// <summary>Destination email address; carried in protected notification metadata.</summary>
    public required string Email { get; init; }

    public required string Code { get; init; }

    /// <summary>True for enrollment verification, false for a sign-in challenge.</summary>
    public bool IsEnrollment { get; init; }
}

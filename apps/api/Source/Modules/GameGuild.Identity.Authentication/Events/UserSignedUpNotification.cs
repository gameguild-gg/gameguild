using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Notification for user sign-up with additional details
/// </summary>
public sealed class UserSignedUpNotification : INotification
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required string Username { get; init; }

    public Guid? TenantId { get; init; }
}

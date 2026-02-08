using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Domain event raised when a user is deactivated
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
public record UserDeactivatedNotification(Guid UserId, string Email, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public int Version { get; } = 1;
}

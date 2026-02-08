using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Domain event raised when a user is deleted
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record UserDeletedNotification(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public int Version { get; } = 1;
}

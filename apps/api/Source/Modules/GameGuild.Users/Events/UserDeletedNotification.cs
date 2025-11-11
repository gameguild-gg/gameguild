using GameGuild.CQRS;

namespace GameGuild.Users.Events;

/// <summary>
///     Domain event raised when a user is deleted
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record UserDeletedNotification(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public int Version { get; } = 1;
}

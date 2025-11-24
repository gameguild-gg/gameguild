using GameGuild.CQRS;

namespace GameGuild.Users.Events;

/// <summary>
///     Domain event raised when a user is permanently deleted (purged)
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
/// <param name="PurgeStrategy">The purge strategy used</param>
public record UserPurgedNotification(Guid UserId, string Email, string Name, string PurgeStrategy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public int Version { get; } = 1;
}

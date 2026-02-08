using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Domain event raised when a user is suspended
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
/// <param name="Reason">Optional reason for suspension</param>
public record UserSuspendedNotification(Guid UserId, string Email, string Name, string? Reason = null) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public int Version { get; } = 1;
}

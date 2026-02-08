using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Domain event raised when a user is updated
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Name">Updated user name</param>
/// <param name="PhoneNumber">Updated phone number</param>
public record UserUpdatedNotification(Guid UserId, string Name, string? PhoneNumber = null) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public int Version { get; } = 1;
}

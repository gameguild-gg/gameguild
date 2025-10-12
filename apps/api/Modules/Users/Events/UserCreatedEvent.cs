using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Event raised when a user is created </summary>
public sealed class UserCreatedEvent(Guid userId, string email, string? givenName, string? familyName, DateTime createdAt) : DomainEventBase(userId, nameof(User))
{
    public Guid UserId { get; } = userId;

    public string Email { get; } = email;

    public string? GivenName { get; } = givenName;

    public string? FamilyName { get; } = familyName;

    public DateTime CreatedAt { get; } = createdAt;
}

using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user is created
/// </summary>
public sealed class UserCreatedEvent(Guid userId, string email, string name, DateTime createdAt)
  : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;

    public string Email { get; } = email;

    public string Name { get; } = name;

    public DateTime CreatedAt { get; } = createdAt;
}

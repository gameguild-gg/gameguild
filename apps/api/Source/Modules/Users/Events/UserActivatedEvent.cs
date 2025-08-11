using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user is activated
/// </summary>
public sealed class UserActivatedEvent(Guid userId) : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;
}

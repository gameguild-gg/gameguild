using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user is deactivated
/// </summary>
public sealed class UserDeactivatedEvent(Guid userId) : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;
}

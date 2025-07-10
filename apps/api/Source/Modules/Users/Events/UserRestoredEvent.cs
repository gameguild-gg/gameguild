using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user is restored
/// </summary>
public sealed class UserRestoredEvent(Guid userId) : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;
}

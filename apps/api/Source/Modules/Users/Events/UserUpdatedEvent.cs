using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user is updated
/// </summary>
public sealed class UserUpdatedEvent(Guid userId, Dictionary<string, object> changes) : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;

  public Dictionary<string, object> Changes { get; } = changes;
}

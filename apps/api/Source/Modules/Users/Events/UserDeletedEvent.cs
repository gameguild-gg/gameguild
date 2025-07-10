using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Event raised when a user is deleted
/// </summary>
public sealed class UserDeletedEvent(Guid userId, bool isSoftDelete) : DomainEventBase(userId, nameof(User)) {
  public Guid UserId { get; } = userId;

  public bool IsSoftDelete { get; } = isSoftDelete;
}
using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Event raised when a user profile is updated
/// </summary>
public sealed class UserProfileUpdatedEvent(
  Guid userProfileId,
  Guid userId,
  Dictionary<string, object> changes,
  DateTime updatedAt
) : DomainEventBase(userProfileId, nameof(UserProfile)) {
  public Guid UserProfileId { get; } = userProfileId;

  public Guid UserId { get; } = userId;

  public Dictionary<string, object> Changes { get; } = changes;

  public DateTime UpdatedAt { get; } = updatedAt;
}

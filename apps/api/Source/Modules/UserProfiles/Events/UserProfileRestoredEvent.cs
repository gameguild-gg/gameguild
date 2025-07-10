using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Event raised when a user profile is restored
/// </summary>
public sealed class UserProfileRestoredEvent(
  Guid userProfileId,
  Guid userId,
  DateTime restoredAt
) : DomainEventBase(userProfileId, nameof(UserProfile)) {
  public Guid UserProfileId { get; } = userProfileId;

  public Guid UserId { get; } = userId;

  public DateTime RestoredAt { get; } = restoredAt;
}

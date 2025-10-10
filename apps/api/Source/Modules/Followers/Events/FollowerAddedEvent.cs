using GameGuild.Modules.Users;
namespace GameGuild.Modules.Followers.Events;

/// <summary> Event raised when a user follows an entity </summary>
public record FollowerAddedEvent(
    Guid FollowerId,
    Guid UserId,
    Guid FollowedEntityId,
    string FollowedEntityType,
    DateTime FollowedAt,
    bool NotificationsEnabled
);

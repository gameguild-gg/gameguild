using GameGuild.Modules.Users;
namespace GameGuild.Modules.Followers.Events;

/// <summary> Event raised when a user unfollows an entity </summary>
public record FollowerRemovedEvent(
    Guid FollowerId,
    Guid UserId,
    Guid FollowedEntityId,
    string FollowedEntityType,
    DateTime UnfollowedAt
);

using GameGuild.Common;
using GameGuild.Modules.UserProfiles.Entities;

namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Event raised when a user profile is deleted
/// </summary>
public sealed class UserProfileDeletedEvent(
    Guid userProfileId,
    Guid userId,
    bool isSoftDelete,
    DateTime deletedAt) : DomainEventBase(userProfileId, nameof(UserProfile))
{
    public Guid UserProfileId { get; } = userProfileId;
    public Guid UserId { get; } = userId;
    public bool IsSoftDelete { get; } = isSoftDelete;
    public DateTime DeletedAt { get; } = deletedAt;
}

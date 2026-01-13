using GameGuild.Entities;

namespace GameGuild.Social.Follows;

/// <summary>
/// Represents a follow relationship between users
/// </summary>
public class Follow : EntityBase
{
    public Guid FollowerId { get; private set; }
    public Guid FollowingId { get; private set; }
    public bool NotificationsEnabled { get; private set; }

    private Follow() { } // EF Core

    public static Follow Create(Guid followerId, Guid followingId)
    {
        return new Follow
        {
            Id = Guid.NewGuid(),
            FollowerId = followerId,
            FollowingId = followingId,
            NotificationsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void EnableNotifications()
    {
        NotificationsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableNotifications()
    {
        NotificationsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a blocked user relationship
/// </summary>
public class Block : EntityBase
{
    public Guid BlockerId { get; private set; }
    public Guid BlockedId { get; private set; }
    public string? Reason { get; private set; }

    private Block() { } // EF Core

    public static Block Create(Guid blockerId, Guid blockedId, string? reason = null)
    {
        return new Block
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedId = blockedId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

using GameGuild.Social.Follows.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Handles core follow/unfollow operations, follower queries, and batch lookups.
/// </summary>
public class FollowOperationService(
    IApplicationDbContext context,
    IUserModerationService moderationService,
    ILogger<FollowOperationService> logger) : IFollowOperationService
{
    #region Follow Operations

    public async Task<Result<Follow>> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true, CancellationToken ct = default)
    {
        // Check if already following
        var existing = await context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct).ConfigureAwait(false);

        if (existing != null)
        {
            return Result<Follow>.Success(existing);
        }

        // If following a user, check if blocked
        if (entityType == FollowableEntityTypes.User)
        {
            var isBlockedResult = await moderationService.AreUsersBlockedAsync(userId, entityId, ct).ConfigureAwait(false);
            if (isBlockedResult.IsSuccess && isBlockedResult.Value)
            {
                return Result<Follow>.Failure<Follow>(FollowerErrors.CannotFollowBlockedUser);
            }

            // Check privacy settings
            var privacySettings = await context.Set<FollowPrivacySettings>()
                .FirstOrDefaultAsync(p => p.UserId == entityId, ct).ConfigureAwait(false);

            if (privacySettings != null && !privacySettings.AllowFollowers)
            {
                return Result<Follow>.Failure<Follow>(FollowerErrors.UserDoesNotAllowFollowers);
            }
        }

        var follow = Follow.Create(userId, entityId, entityType, notificationsEnabled);
        context.Set<Follow>().Add(follow);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {UserId} followed {EntityType} {EntityId}", userId, entityType, entityId);

        return Result<Follow>.Success(follow);
    }

    public async Task<Result> UnfollowAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default)
    {
        var follow = await context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct).ConfigureAwait(false);

        if (follow == null)
        {
            return Result.Failure(FollowerErrors.FollowNotFound);
        }

        context.Set<Follow>().Remove(follow);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {UserId} unfollowed {EntityType} {EntityId}", userId, entityType, entityId);

        return Result.Success();
    }

    public async Task<Result<bool>> IsFollowingAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default)
    {
        var isFollowing = await context.Set<Follow>()
            .AnyAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct).ConfigureAwait(false);

        return Result<bool>.Success(isFollowing);
    }

    public async Task<Result<Follow>> UpdateNotificationSettingsAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled, CancellationToken ct = default)
    {
        var follow = await context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct).ConfigureAwait(false);

        if (follow == null)
        {
            return Result<Follow>.Failure<Follow>(FollowerErrors.FollowNotFound);
        }

        follow.UpdateNotificationSettings(notificationsEnabled);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<Follow>.Success(follow);
    }

    #endregion

    #region Query Operations

    public async Task<Result<List<Follow>>> GetFollowersAsync(Guid entityId, string entityType, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var followers = await context.Set<Follow>()
            .Where(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType)
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct).ConfigureAwait(false);

        return Result<List<Follow>>.Success(followers);
    }

    public async Task<Result<List<Follow>>> GetFollowingAsync(Guid userId, string? entityType = null, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var query = context.Set<Follow>()
            .Where(f => f.FollowerId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(f => f.FollowedEntityType == entityType);
        }

        var following = await query
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct).ConfigureAwait(false);

        return Result<List<Follow>>.Success(following);
    }

    public async Task<Result<int>> GetFollowerCountAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var count = await context.Set<Follow>()
            .CountAsync(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct).ConfigureAwait(false);

        return Result<int>.Success(count);
    }

    public async Task<Result<int>> GetFollowingCountAsync(Guid userId, string? entityType = null, CancellationToken ct = default)
    {
        var query = context.Set<Follow>()
            .Where(f => f.FollowerId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(f => f.FollowedEntityType == entityType);
        }

        var count = await query.CountAsync(ct).ConfigureAwait(false);
        return Result<int>.Success(count);
    }

    public async Task<Result<bool>> AreMutualFollowersAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var user1FollowsUser2 = await context.Set<Follow>()
            .AnyAsync(f => f.FollowerId == userId1 && f.FollowedEntityId == userId2 && f.FollowedEntityType == FollowableEntityTypes.User, ct).ConfigureAwait(false);

        if (!user1FollowsUser2)
        {
            return Result<bool>.Success(false);
        }

        var user2FollowsUser1 = await context.Set<Follow>()
            .AnyAsync(f => f.FollowerId == userId2 && f.FollowedEntityId == userId1 && f.FollowedEntityType == FollowableEntityTypes.User, ct).ConfigureAwait(false);

        return Result<bool>.Success(user2FollowsUser1);
    }

    public async Task<Result<Follow>> GetFollowByIdAsync(Guid followId, CancellationToken ct = default)
    {
        var follow = await context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.Id == followId, ct).ConfigureAwait(false);

        if (follow == null)
        {
            return Result<Follow>.Failure<Follow>(FollowerErrors.FollowNotFound);
        }

        return Result<Follow>.Success(follow);
    }

    public async Task<Result<List<Follow>>> GetFollowersWithNotificationsAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var followers = await context.Set<Follow>()
            .Where(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType && f.NotificationsEnabled)
            .ToListAsync(ct).ConfigureAwait(false);

        return Result<List<Follow>>.Success(followers);
    }

    #endregion

    #region Batch Operations

    public async Task<Result<Dictionary<Guid, bool>>> GetFollowStatusBatchAsync(Guid userId, IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
    {
        var entityIdList = entityIds.ToList();

        var followedIds = await context.Set<Follow>()
            .Where(f => f.FollowerId == userId && f.FollowedEntityType == entityType && entityIdList.Contains(f.FollowedEntityId))
            .Select(f => f.FollowedEntityId)
            .ToListAsync(ct).ConfigureAwait(false);

        var result = entityIdList.ToDictionary(id => id, id => followedIds.Contains(id));
        return Result<Dictionary<Guid, bool>>.Success(result);
    }

    public async Task<Result<Dictionary<Guid, int>>> GetFollowerCountsBatchAsync(IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
    {
        var entityIdList = entityIds.ToList();

        var counts = await context.Set<Follow>()
            .Where(f => f.FollowedEntityType == entityType && entityIdList.Contains(f.FollowedEntityId))
            .GroupBy(f => f.FollowedEntityId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityId, x => x.Count, ct).ConfigureAwait(false);

        // Include entities with 0 followers
        var result = entityIdList.ToDictionary(id => id, id => counts.GetValueOrDefault(id, 0));
        return Result<Dictionary<Guid, int>>.Success(result);
    }

    #endregion
}

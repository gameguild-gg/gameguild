using GameGuild.Abstractions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Models;
using GameGuild.Social.Follows.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Service implementation for managing follower relationships, blocking, muting, and privacy settings
/// </summary>
public class FollowerService : IFollowerService
{
    private readonly IApplicationDbContext _context;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<FollowerService> _logger;

    public FollowerService(
        IApplicationDbContext context,
        IActorContextAccessor actorContextAccessor,
        ILogger<FollowerService> logger)
    {
        _context = context;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;

    #region Follow Operations

    public async Task<Result<Follow>> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true, CancellationToken ct = default)
    {
        // Check if already following
        var existing = await _context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct);

        if (existing != null)
        {
            return Result<Follow>.Success(existing);
        }

        // If following a user, check if blocked
        if (entityType == FollowableEntityTypes.User)
        {
            var isBlockedResult = await AreUsersBlockedAsync(userId, entityId, ct);
            if (isBlockedResult.IsSuccess && isBlockedResult.Value)
            {
                return Result<Follow>.Failure<Follow>(FollowerErrors.CannotFollowBlockedUser);
            }

            // Check privacy settings
            var privacySettings = await _context.Set<FollowPrivacySettings>()
                .FirstOrDefaultAsync(p => p.UserId == entityId, ct);

            if (privacySettings != null && !privacySettings.AllowFollowers)
            {
                return Result<Follow>.Failure<Follow>(FollowerErrors.UserDoesNotAllowFollowers);
            }
        }

        var follow = Follow.Create(userId, entityId, entityType, notificationsEnabled);
        _context.Set<Follow>().Add(follow);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} followed {EntityType} {EntityId}", userId, entityType, entityId);

        return Result<Follow>.Success(follow);
    }

    public async Task<Result> UnfollowAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default)
    {
        var follow = await _context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct);

        if (follow == null)
        {
            return Result.Failure(FollowerErrors.FollowNotFound);
        }

        _context.Set<Follow>().Remove(follow);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} unfollowed {EntityType} {EntityId}", userId, entityType, entityId);

        return Result.Success();
    }

    public async Task<Result<bool>> IsFollowingAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default)
    {
        var isFollowing = await _context.Set<Follow>()
            .AnyAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct);

        return Result<bool>.Success(isFollowing);
    }

    public async Task<Result<Follow>> UpdateNotificationSettingsAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled, CancellationToken ct = default)
    {
        var follow = await _context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct);

        if (follow == null)
        {
            return Result<Follow>.Failure<Follow>(FollowerErrors.FollowNotFound);
        }

        follow.UpdateNotificationSettings(notificationsEnabled);
        await _context.SaveChangesAsync(ct);

        return Result<Follow>.Success(follow);
    }

    #endregion

    #region Query Operations

    public async Task<Result<List<Follow>>> GetFollowersAsync(Guid entityId, string entityType, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var followers = await _context.Set<Follow>()
            .Where(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType)
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result<List<Follow>>.Success(followers);
    }

    public async Task<Result<List<Follow>>> GetFollowingAsync(Guid userId, string? entityType = null, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var query = _context.Set<Follow>()
            .Where(f => f.FollowerId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(f => f.FollowedEntityType == entityType);
        }

        var following = await query
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result<List<Follow>>.Success(following);
    }

    public async Task<Result<int>> GetFollowerCountAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var count = await _context.Set<Follow>()
            .CountAsync(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType, ct);

        return Result<int>.Success(count);
    }

    public async Task<Result<int>> GetFollowingCountAsync(Guid userId, string? entityType = null, CancellationToken ct = default)
    {
        var query = _context.Set<Follow>()
            .Where(f => f.FollowerId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(f => f.FollowedEntityType == entityType);
        }

        var count = await query.CountAsync(ct);
        return Result<int>.Success(count);
    }

    public async Task<Result<bool>> AreMutualFollowersAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var user1FollowsUser2 = await _context.Set<Follow>()
            .AnyAsync(f => f.FollowerId == userId1 && f.FollowedEntityId == userId2 && f.FollowedEntityType == FollowableEntityTypes.User, ct);

        if (!user1FollowsUser2)
        {
            return Result<bool>.Success(false);
        }

        var user2FollowsUser1 = await _context.Set<Follow>()
            .AnyAsync(f => f.FollowerId == userId2 && f.FollowedEntityId == userId1 && f.FollowedEntityType == FollowableEntityTypes.User, ct);

        return Result<bool>.Success(user2FollowsUser1);
    }

    public async Task<Result<Follow>> GetFollowByIdAsync(Guid followId, CancellationToken ct = default)
    {
        var follow = await _context.Set<Follow>()
            .FirstOrDefaultAsync(f => f.Id == followId, ct);

        if (follow == null)
        {
            return Result<Follow>.Failure<Follow>(FollowerErrors.FollowNotFound);
        }

        return Result<Follow>.Success(follow);
    }

    public async Task<Result<List<Follow>>> GetFollowersWithNotificationsAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var followers = await _context.Set<Follow>()
            .Where(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType && f.NotificationsEnabled)
            .ToListAsync(ct);

        return Result<List<Follow>>.Success(followers);
    }

    #endregion

    #region Batch Operations

    public async Task<Result<Dictionary<Guid, bool>>> GetFollowStatusBatchAsync(Guid userId, IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
    {
        var entityIdList = entityIds.ToList();

        var followedIds = await _context.Set<Follow>()
            .Where(f => f.FollowerId == userId && f.FollowedEntityType == entityType && entityIdList.Contains(f.FollowedEntityId))
            .Select(f => f.FollowedEntityId)
            .ToListAsync(ct);

        var result = entityIdList.ToDictionary(id => id, id => followedIds.Contains(id));
        return Result<Dictionary<Guid, bool>>.Success(result);
    }

    public async Task<Result<Dictionary<Guid, int>>> GetFollowerCountsBatchAsync(IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
    {
        var entityIdList = entityIds.ToList();

        var counts = await _context.Set<Follow>()
            .Where(f => f.FollowedEntityType == entityType && entityIdList.Contains(f.FollowedEntityId))
            .GroupBy(f => f.FollowedEntityId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityId, x => x.Count, ct);

        // Include entities with 0 followers
        var result = entityIdList.ToDictionary(id => id, id => counts.GetValueOrDefault(id, 0));
        return Result<Dictionary<Guid, int>>.Success(result);
    }

    #endregion

    #region Privacy Settings

    public async Task<Result<FollowPrivacySettings>> GetPrivacySettingsAsync(Guid userId, CancellationToken ct = default)
    {
        var settings = await _context.Set<FollowPrivacySettings>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (settings == null)
        {
            // Create default settings
            settings = FollowPrivacySettings.CreateDefault(userId);
            _context.Set<FollowPrivacySettings>().Add(settings);
            await _context.SaveChangesAsync(ct);
        }

        return Result<FollowPrivacySettings>.Success(settings);
    }

    public async Task<Result<FollowPrivacySettings>> UpdatePrivacySettingsAsync(
        Guid userId,
        bool isFollowerListPublic,
        bool isFollowingListPublic,
        bool allowFollowers,
        bool notifyOnNewFollower,
        bool showFollowerCount,
        bool showFollowingCount,
        CancellationToken ct = default)
    {
        var settings = await _context.Set<FollowPrivacySettings>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (settings == null)
        {
            settings = FollowPrivacySettings.CreateDefault(userId);
            _context.Set<FollowPrivacySettings>().Add(settings);
        }

        settings.Update(
            isFollowerListPublic,
            isFollowingListPublic,
            allowFollowers,
            notifyOnNewFollower,
            showFollowerCount,
            showFollowingCount);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} updated privacy settings", userId);

        return Result<FollowPrivacySettings>.Success(settings);
    }

    #endregion

    #region Block Operations

    public async Task<Result<Block>> BlockUserAsync(Guid blockingUserId, Guid blockedUserId, string? reason = null, CancellationToken ct = default)
    {
        if (blockingUserId == blockedUserId)
        {
            return Result<Block>.Failure<Block>(FollowerErrors.CannotBlockYourself);
        }

        // Check if already blocked
        var existing = await _context.Set<Block>()
            .FirstOrDefaultAsync(b => b.BlockerId == blockingUserId && b.BlockedId == blockedUserId, ct);

        if (existing != null)
        {
            return Result<Block>.Success(existing);
        }

        // Remove any existing follow relationships between these users
        var followsToRemove = await _context.Set<Follow>()
            .Where(f =>
                (f.FollowerId == blockingUserId && f.FollowedEntityId == blockedUserId && f.FollowedEntityType == FollowableEntityTypes.User) ||
                (f.FollowerId == blockedUserId && f.FollowedEntityId == blockingUserId && f.FollowedEntityType == FollowableEntityTypes.User))
            .ToListAsync(ct);

        _context.Set<Follow>().RemoveRange(followsToRemove);

        var block = Block.Create(blockingUserId, blockedUserId, reason);
        _context.Set<Block>().Add(block);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {BlockingUserId} blocked user {BlockedUserId}", blockingUserId, blockedUserId);

        return Result<Block>.Success(block);
    }

    public async Task<Result> UnblockUserAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        var block = await _context.Set<Block>()
            .FirstOrDefaultAsync(b => b.BlockerId == blockingUserId && b.BlockedId == blockedUserId, ct);

        if (block == null)
        {
            return Result.Failure(FollowerErrors.BlockNotFound);
        }

        _context.Set<Block>().Remove(block);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {BlockingUserId} unblocked user {BlockedUserId}", blockingUserId, blockedUserId);

        return Result.Success();
    }

    public async Task<Result<bool>> IsUserBlockedAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        var isBlocked = await _context.Set<Block>()
            .AnyAsync(b => b.BlockerId == blockingUserId && b.BlockedId == blockedUserId, ct);

        return Result<bool>.Success(isBlocked);
    }

    public async Task<Result<bool>> AreUsersBlockedAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var areBlocked = await _context.Set<Block>()
            .AnyAsync(b =>
                (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                (b.BlockerId == userId2 && b.BlockedId == userId1), ct);

        return Result<bool>.Success(areBlocked);
    }

    public async Task<Result<List<Block>>> GetBlockedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var blocks = await _context.Set<Block>()
            .Where(b => b.BlockerId == userId)
            .OrderByDescending(b => b.BlockedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result<List<Block>>.Success(blocks);
    }

    #endregion

    #region Mute Operations

    public async Task<Result<Mute>> MuteUserAsync(Guid mutingUserId, Guid mutedUserId, string? reason = null, DateTime? expiresAt = null, CancellationToken ct = default)
    {
        if (mutingUserId == mutedUserId)
        {
            return Result<Mute>.Failure<Mute>(FollowerErrors.CannotMuteYourself);
        }

        // Check if already muted
        var existing = await _context.Set<Mute>()
            .FirstOrDefaultAsync(m => m.MuterId == mutingUserId && m.MutedId == mutedUserId, ct);

        if (existing != null)
        {
            // Update expiration if provided
            if (expiresAt.HasValue)
            {
                existing.ExtendExpiration(expiresAt);
                await _context.SaveChangesAsync(ct);
            }
            return Result<Mute>.Success(existing);
        }

        var mute = Mute.Create(mutingUserId, mutedUserId, reason, expiresAt);
        _context.Set<Mute>().Add(mute);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {MutingUserId} muted user {MutedUserId}", mutingUserId, mutedUserId);

        return Result<Mute>.Success(mute);
    }

    public async Task<Result> UnmuteUserAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default)
    {
        var mute = await _context.Set<Mute>()
            .FirstOrDefaultAsync(m => m.MuterId == mutingUserId && m.MutedId == mutedUserId, ct);

        if (mute == null)
        {
            return Result.Failure(FollowerErrors.MuteNotFound);
        }

        _context.Set<Mute>().Remove(mute);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {MutingUserId} unmuted user {MutedUserId}", mutingUserId, mutedUserId);

        return Result.Success();
    }

    public async Task<Result<bool>> IsUserMutedAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default)
    {
        var mute = await _context.Set<Mute>()
            .FirstOrDefaultAsync(m => m.MuterId == mutingUserId && m.MutedId == mutedUserId, ct);

        if (mute == null)
        {
            return Result<bool>.Success(false);
        }

        // Check if mute has expired
        if (mute.IsExpired())
        {
            _context.Set<Mute>().Remove(mute);
            await _context.SaveChangesAsync(ct);
            return Result<bool>.Success(false);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<List<Mute>>> GetMutedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var mutes = await _context.Set<Mute>()
            .Where(m => m.MuterId == userId && (!m.ExpiresAt.HasValue || m.ExpiresAt.Value > now))
            .OrderByDescending(m => m.MutedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result<List<Mute>>.Success(mutes);
    }

    public async Task<Result<int>> CleanupExpiredMutesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expiredMutes = await _context.Set<Mute>()
            .Where(m => m.ExpiresAt.HasValue && m.ExpiresAt.Value <= now)
            .ToListAsync(ct);

        if (expiredMutes.Count > 0)
        {
            _context.Set<Mute>().RemoveRange(expiredMutes);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Cleaned up {Count} expired mutes", expiredMutes.Count);
        }

        return Result<int>.Success(expiredMutes.Count);
    }

    #endregion
}

/// <summary>
/// Error codes for follower operations
/// </summary>
public static class FollowerErrors
{
    public static readonly Error FollowNotFound = Error.NotFound("Follower.NotFound", "Follow relationship not found");
    public static readonly Error BlockNotFound = Error.NotFound("Block.NotFound", "Block relationship not found");
    public static readonly Error MuteNotFound = Error.NotFound("Mute.NotFound", "Mute relationship not found");
    public static readonly Error CannotFollowBlockedUser = Error.Failure("Follower.Blocked", "Cannot follow a user who has blocked you or you have blocked");
    public static readonly Error UserDoesNotAllowFollowers = Error.Failure("Follower.NotAllowed", "This user does not allow followers");
    public static readonly Error CannotBlockYourself = Error.Failure("Block.Self", "Cannot block yourself");
    public static readonly Error CannotMuteYourself = Error.Failure("Mute.Self", "Cannot mute yourself");
}

/// <summary>
/// Constants for followable entity types
/// </summary>
public static class FollowableEntityTypes
{
    public const string User = "User";
    public const string Course = "Course";
    public const string Project = "Project";
    public const string Program = "Program";
    public const string Tag = "Tag";
    public const string Team = "Team";
}

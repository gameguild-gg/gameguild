using GameGuild.Modules.Users;
using GameGuild.Database;
using GameGuild.Modules.Followers.Entities;
using GameGuild.Modules.Followers.Events;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Followers.Services;

/// <summary> Service implementation for managing follower relationships </summary>
public class FollowerService : IFollowerService
{
    private readonly ApplicationDbContext _context;

    public FollowerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Follower> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true)
    {
        // Check if already following
        var existing = await _context.Set<Follower>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType);

        if (existing != null)
        {
            return existing;
        }

        // Check if user is blocked
        var isBlocked = await IsUserBlockedAsync(entityId, userId);
        if (isBlocked)
        {
            throw new InvalidOperationException("Cannot follow a user who has blocked you.");
        }

        // Check privacy settings if following a user
        if (entityType == "User")
        {
            var privacySettings = await GetPrivacySettingsAsync(entityId);
            if (privacySettings != null && !privacySettings.AllowFollowers)
            {
                throw new InvalidOperationException("This user does not allow followers.");
            }
        }

        var follower = new Follower
        {
            UserId = userId,
            FollowedEntityId = entityId,
            FollowedEntityType = entityType,
            NotificationsEnabled = notificationsEnabled,
            FollowedAt = DateTime.UtcNow
        };

        _context.Set<Follower>().Add(follower);
        await _context.SaveChangesAsync();

        // Publish domain event
        var domainEvent = new FollowerAddedEvent(
            follower.Id,
            userId,
            entityId,
            entityType,
            follower.FollowedAt,
            notificationsEnabled
        );

        return follower;
    }

    public async Task<bool> UnfollowAsync(Guid userId, Guid entityId, string entityType)
    {
        var follower = await _context.Set<Follower>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType);

        if (follower == null)
        {
            return false;
        }

        _context.Set<Follower>().Remove(follower);
        await _context.SaveChangesAsync();

        // Publish domain event
        var domainEvent = new FollowerRemovedEvent(
            follower.Id,
            userId,
            entityId,
            entityType,
            DateTime.UtcNow
        );

        return true;
    }

    public async Task<bool> IsFollowingAsync(Guid userId, Guid entityId, string entityType)
    {
        return await _context.Set<Follower>()
            .AnyAsync(f => f.UserId == userId && f.FollowedEntityId == entityId && f.FollowedEntityType == entityType);
    }

    public async Task<IEnumerable<Follower>> GetFollowersAsync(Guid entityId, string entityType, int skip = 0, int take = 50)
    {
        return await _context.Set<Follower>()
            .Where(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType)
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(take)
            .Include(f => f.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<Follower>> GetFollowingAsync(Guid userId, string? entityType = null, int skip = 0, int take = 50)
    {
        var query = _context.Set<Follower>()
            .Where(f => f.UserId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(f => f.FollowedEntityType == entityType);
        }

        return await query
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetFollowerCountAsync(Guid entityId, string entityType)
    {
        return await _context.Set<Follower>()
            .CountAsync(f => f.FollowedEntityId == entityId && f.FollowedEntityType == entityType);
    }

    public async Task<int> GetFollowingCountAsync(Guid userId, string? entityType = null)
    {
        var query = _context.Set<Follower>()
            .Where(f => f.UserId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(f => f.FollowedEntityType == entityType);
        }

        return await query.CountAsync();
    }

    public async Task<bool> AreMutualFollowersAsync(Guid userId1, Guid userId2)
    {
        var user1FollowsUser2 = await _context.Set<Follower>()
            .AnyAsync(f => f.UserId == userId1 && f.FollowedEntityId == userId2 && f.FollowedEntityType == "User");

        if (!user1FollowsUser2)
        {
            return false;
        }

        var user2FollowsUser1 = await _context.Set<Follower>()
            .AnyAsync(f => f.UserId == userId2 && f.FollowedEntityId == userId1 && f.FollowedEntityType == "User");

        return user2FollowsUser1;
    }

    public async Task<FollowerPrivacySettings?> GetPrivacySettingsAsync(Guid userId)
    {
        var settings = await _context.Set<FollowerPrivacySettings>()
            .FirstOrDefaultAsync(fps => fps.UserId == userId);

        // Create default settings if none exist
        if (settings == null)
        {
            settings = new FollowerPrivacySettings
            {
                UserId = userId,
                IsFollowerListPublic = true,
                IsFollowingListPublic = true,
                AllowFollowers = true,
                NotifyOnNewFollower = true,
                ShowFollowerCount = true,
                ShowFollowingCount = true
            };

            _context.Set<FollowerPrivacySettings>().Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task<FollowerPrivacySettings> UpdatePrivacySettingsAsync(Guid userId, FollowerPrivacySettings settings)
    {
        var existing = await _context.Set<FollowerPrivacySettings>()
            .FirstOrDefaultAsync(fps => fps.UserId == userId);

        if (existing == null)
        {
            settings.UserId = userId;
            _context.Set<FollowerPrivacySettings>().Add(settings);
        }
        else
        {
            existing.IsFollowerListPublic = settings.IsFollowerListPublic;
            existing.IsFollowingListPublic = settings.IsFollowingListPublic;
            existing.AllowFollowers = settings.AllowFollowers;
            existing.NotifyOnNewFollower = settings.NotifyOnNewFollower;
            existing.ShowFollowerCount = settings.ShowFollowerCount;
            existing.ShowFollowingCount = settings.ShowFollowingCount;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return existing ?? settings;
    }

    public async Task<BlockedUser> BlockUserAsync(Guid blockingUserId, Guid blockedUserId, string? reason = null)
    {
        if (blockingUserId == blockedUserId)
        {
            throw new InvalidOperationException("Cannot block yourself.");
        }

        // Check if already blocked
        var existing = await _context.Set<BlockedUser>()
            .FirstOrDefaultAsync(bu => bu.BlockingUserId == blockingUserId && bu.BlockedUserId == blockedUserId);

        if (existing != null)
        {
            return existing;
        }

        // Remove any existing follow relationships
        var followRelationships = await _context.Set<Follower>()
            .Where(f => (f.UserId == blockingUserId && f.FollowedEntityId == blockedUserId && f.FollowedEntityType == "User") ||
                       (f.UserId == blockedUserId && f.FollowedEntityId == blockingUserId && f.FollowedEntityType == "User"))
            .ToListAsync();

        _context.Set<Follower>().RemoveRange(followRelationships);

        var blockedUser = new BlockedUser
        {
            BlockingUserId = blockingUserId,
            BlockedUserId = blockedUserId,
            Reason = reason,
            BlockedAt = DateTime.UtcNow
        };

        _context.Set<BlockedUser>().Add(blockedUser);
        await _context.SaveChangesAsync();

        return blockedUser;
    }

    public async Task<bool> UnblockUserAsync(Guid blockingUserId, Guid blockedUserId)
    {
        var blockedUser = await _context.Set<BlockedUser>()
            .FirstOrDefaultAsync(bu => bu.BlockingUserId == blockingUserId && bu.BlockedUserId == blockedUserId);

        if (blockedUser == null)
        {
            return false;
        }

        _context.Set<BlockedUser>().Remove(blockedUser);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsUserBlockedAsync(Guid blockingUserId, Guid blockedUserId)
    {
        return await _context.Set<BlockedUser>()
            .AnyAsync(bu => bu.BlockingUserId == blockingUserId && bu.BlockedUserId == blockedUserId);
    }

    public async Task<MutedUser> MuteUserAsync(Guid mutingUserId, Guid mutedUserId, string? reason = null, DateTime? expiresAt = null)
    {
        if (mutingUserId == mutedUserId)
        {
            throw new InvalidOperationException("Cannot mute yourself.");
        }

        // Check if already muted
        var existing = await _context.Set<MutedUser>()
            .FirstOrDefaultAsync(mu => mu.MutingUserId == mutingUserId && mu.MutedUserId == mutedUserId);

        if (existing != null)
        {
            // Update expiration if provided
            if (expiresAt.HasValue)
            {
                existing.ExpiresAt = expiresAt;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        var mutedUser = new MutedUser
        {
            MutingUserId = mutingUserId,
            MutedUserId = mutedUserId,
            Reason = reason,
            MutedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        _context.Set<MutedUser>().Add(mutedUser);
        await _context.SaveChangesAsync();

        return mutedUser;
    }

    public async Task<bool> UnmuteUserAsync(Guid mutingUserId, Guid mutedUserId)
    {
        var mutedUser = await _context.Set<MutedUser>()
            .FirstOrDefaultAsync(mu => mu.MutingUserId == mutingUserId && mu.MutedUserId == mutedUserId);

        if (mutedUser == null)
        {
            return false;
        }

        _context.Set<MutedUser>().Remove(mutedUser);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsUserMutedAsync(Guid mutingUserId, Guid mutedUserId)
    {
        var mutedUser = await _context.Set<MutedUser>()
            .FirstOrDefaultAsync(mu => mu.MutingUserId == mutingUserId && mu.MutedUserId == mutedUserId);

        if (mutedUser == null)
        {
            return false;
        }

        // Check if mute has expired
        if (mutedUser.ExpiresAt.HasValue && mutedUser.ExpiresAt.Value <= DateTime.UtcNow)
        {
            _context.Set<MutedUser>().Remove(mutedUser);
            await _context.SaveChangesAsync();
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<BlockedUser>> GetBlockedUsersAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.Set<BlockedUser>()
            .Where(bu => bu.BlockingUserId == userId)
            .OrderByDescending(bu => bu.BlockedAt)
            .Skip(skip)
            .Take(take)
            .Include(bu => bu.BlockedUserEntity)
            .ToListAsync();
    }

    public async Task<IEnumerable<MutedUser>> GetMutedUsersAsync(Guid userId, int skip = 0, int take = 50)
    {
        var mutedUsers = await _context.Set<MutedUser>()
            .Where(mu => mu.MutingUserId == userId)
            .OrderByDescending(mu => mu.MutedAt)
            .Skip(skip)
            .Take(take)
            .Include(mu => mu.MutedUserEntity)
            .ToListAsync();

        // Filter out expired mutes
        var activeMutes = mutedUsers.Where(mu => !mu.ExpiresAt.HasValue || mu.ExpiresAt.Value > DateTime.UtcNow).ToList();

        return activeMutes;
    }
}

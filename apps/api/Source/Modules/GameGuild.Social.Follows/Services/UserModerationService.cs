using GameGuild.Social.Follows.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Handles user moderation: blocking, muting, and privacy settings.
/// </summary>
public class UserModerationService(
    IApplicationDbContext context,
    ILogger<UserModerationService> logger) : IUserModerationService
{
    #region Privacy Settings

    public async Task<Result<FollowPrivacySettings>> GetPrivacySettingsAsync(Guid userId, CancellationToken ct = default)
    {
        var settings = await context.Set<FollowPrivacySettings>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);

        if (settings == null)
        {
            // Create default settings
            settings = FollowPrivacySettings.CreateDefault(userId);
            context.Set<FollowPrivacySettings>().Add(settings);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
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
        var settings = await context.Set<FollowPrivacySettings>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);

        if (settings == null)
        {
            settings = FollowPrivacySettings.CreateDefault(userId);
            context.Set<FollowPrivacySettings>().Add(settings);
        }

        settings.Update(
            isFollowerListPublic,
            isFollowingListPublic,
            allowFollowers,
            notifyOnNewFollower,
            showFollowerCount,
            showFollowingCount);

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {UserId} updated privacy settings", userId);

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
        var existing = await context.Set<Block>()
            .FirstOrDefaultAsync(b => b.BlockerId == blockingUserId && b.BlockedId == blockedUserId, ct).ConfigureAwait(false);

        if (existing != null)
        {
            return Result<Block>.Success(existing);
        }

        // Remove any existing follow relationships between these users
        var followsToRemove = await context.Set<Follow>()
            .Where(f =>
                (f.FollowerId == blockingUserId && f.FollowedEntityId == blockedUserId && f.FollowedEntityType == FollowableEntityTypes.User) ||
                (f.FollowerId == blockedUserId && f.FollowedEntityId == blockingUserId && f.FollowedEntityType == FollowableEntityTypes.User))
            .ToListAsync(ct).ConfigureAwait(false);

        context.Set<Follow>().RemoveRange(followsToRemove);

        var block = Block.Create(blockingUserId, blockedUserId, reason);
        context.Set<Block>().Add(block);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {BlockingUserId} blocked user {BlockedUserId}", blockingUserId, blockedUserId);

        return Result<Block>.Success(block);
    }

    public async Task<Result> UnblockUserAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        var block = await context.Set<Block>()
            .FirstOrDefaultAsync(b => b.BlockerId == blockingUserId && b.BlockedId == blockedUserId, ct).ConfigureAwait(false);

        if (block == null)
        {
            return Result.Failure(FollowerErrors.BlockNotFound);
        }

        context.Set<Block>().Remove(block);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {BlockingUserId} unblocked user {BlockedUserId}", blockingUserId, blockedUserId);

        return Result.Success();
    }

    public async Task<Result<bool>> IsUserBlockedAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        var isBlocked = await context.Set<Block>()
            .AnyAsync(b => b.BlockerId == blockingUserId && b.BlockedId == blockedUserId, ct).ConfigureAwait(false);

        return Result<bool>.Success(isBlocked);
    }

    public async Task<Result<bool>> AreUsersBlockedAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var areBlocked = await context.Set<Block>()
            .AnyAsync(b =>
                (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                (b.BlockerId == userId2 && b.BlockedId == userId1), ct).ConfigureAwait(false);

        return Result<bool>.Success(areBlocked);
    }

    public async Task<Result<List<Block>>> GetBlockedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var blocks = await context.Set<Block>()
            .Where(b => b.BlockerId == userId)
            .OrderByDescending(b => b.BlockedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct).ConfigureAwait(false);

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
        var existing = await context.Set<Mute>()
            .FirstOrDefaultAsync(m => m.MuterId == mutingUserId && m.MutedId == mutedUserId, ct).ConfigureAwait(false);

        if (existing != null)
        {
            // Update expiration if provided
            if (expiresAt.HasValue)
            {
                existing.ExtendExpiration(expiresAt);
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            return Result<Mute>.Success(existing);
        }

        var mute = Mute.Create(mutingUserId, mutedUserId, reason, expiresAt);
        context.Set<Mute>().Add(mute);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {MutingUserId} muted user {MutedUserId}", mutingUserId, mutedUserId);

        return Result<Mute>.Success(mute);
    }

    public async Task<Result> UnmuteUserAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default)
    {
        var mute = await context.Set<Mute>()
            .FirstOrDefaultAsync(m => m.MuterId == mutingUserId && m.MutedId == mutedUserId, ct).ConfigureAwait(false);

        if (mute == null)
        {
            return Result.Failure(FollowerErrors.MuteNotFound);
        }

        context.Set<Mute>().Remove(mute);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {MutingUserId} unmuted user {MutedUserId}", mutingUserId, mutedUserId);

        return Result.Success();
    }

    public async Task<Result<bool>> IsUserMutedAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default)
    {
        var mute = await context.Set<Mute>()
            .FirstOrDefaultAsync(m => m.MuterId == mutingUserId && m.MutedId == mutedUserId, ct).ConfigureAwait(false);

        if (mute == null)
        {
            return Result<bool>.Success(false);
        }

        // Check if mute has expired
        if (mute.IsExpired())
        {
            context.Set<Mute>().Remove(mute);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result<bool>.Success(false);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<List<Mute>>> GetMutedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var now = SystemClock.UtcNow;
        var mutes = await context.Set<Mute>()
            .Where(m => m.MuterId == userId && (!m.ExpiresAt.HasValue || m.ExpiresAt.Value > now))
            .OrderByDescending(m => m.MutedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct).ConfigureAwait(false);

        return Result<List<Mute>>.Success(mutes);
    }

    public async Task<Result<int>> CleanupExpiredMutesAsync(CancellationToken ct = default)
    {
        var now = SystemClock.UtcNow;
        var expiredMutes = await context.Set<Mute>()
            .Where(m => m.ExpiresAt.HasValue && m.ExpiresAt.Value <= now)
            .ToListAsync(ct).ConfigureAwait(false);

        if (expiredMutes.Count > 0)
        {
            context.Set<Mute>().RemoveRange(expiredMutes);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Cleaned up {Count} expired mutes", expiredMutes.Count);
        }

        return Result<int>.Success(expiredMutes.Count);
    }

    #endregion
}

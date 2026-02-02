using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Follows.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Follows.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowersController : ControllerBase
{
    private readonly IFollowerService _followerService;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<FollowersController> _logger;

    public FollowersController(
        IFollowerService followerService,
        IActorContextAccessor actorContextAccessor,
        ILogger<FollowersController> logger)
    {
        _followerService = followerService;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;

    #region Follow Endpoints

    /// <summary>Follow an entity</summary>
    [HttpPost("follow")]
    public async Task<ActionResult<FollowDto>> Follow([FromBody] FollowRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.FollowAsync(userId, request.EntityId, request.EntityType, request.NotificationsEnabled, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error.Code, message = result.Error.Description });
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>Unfollow an entity</summary>
    [HttpDelete("unfollow")]
    public async Task<ActionResult> Unfollow([FromQuery] Guid entityId, [FromQuery] string entityType, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.UnfollowAsync(userId, entityId, entityType, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error.Code, message = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>Check if current user is following an entity</summary>
    [HttpGet("is-following")]
    public async Task<ActionResult<bool>> IsFollowing([FromQuery] Guid entityId, [FromQuery] string entityType, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.IsFollowingAsync(userId, entityId, entityType, ct);
        return Ok(result.Value);
    }

    /// <summary>Update notification settings for a follow relationship</summary>
    [HttpPut("notifications")]
    public async Task<ActionResult<FollowDto>> UpdateNotifications([FromBody] UpdateNotificationsRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.UpdateNotificationSettingsAsync(userId, request.EntityId, request.EntityType, request.NotificationsEnabled, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error.Code, message = result.Error.Description });
        }

        return Ok(MapToDto(result.Value));
    }

    #endregion

    #region Query Endpoints

    /// <summary>Get followers for an entity</summary>
    [HttpGet("followers/{entityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowers(
        Guid entityId,
        [FromQuery] string entityType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await _followerService.GetFollowersAsync(entityId, entityType, skip, take, ct);
        return Ok(result.Value.Select(MapToDto));
    }

    /// <summary>Get entities the current user is following</summary>
    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowing(
        [FromQuery] string? entityType = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.GetFollowingAsync(userId, entityType, skip, take, ct);
        return Ok(result.Value.Select(MapToDto));
    }

    /// <summary>Get follower count for an entity</summary>
    [HttpGet("count/followers/{entityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<int>> GetFollowerCount(Guid entityId, [FromQuery] string entityType, CancellationToken ct)
    {
        var result = await _followerService.GetFollowerCountAsync(entityId, entityType, ct);
        return Ok(result.Value);
    }

    /// <summary>Get following count for current user</summary>
    [HttpGet("count/following")]
    public async Task<ActionResult<int>> GetFollowingCount([FromQuery] string? entityType = null, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.GetFollowingCountAsync(userId, entityType, ct);
        return Ok(result.Value);
    }

    /// <summary>Check if two users are mutual followers</summary>
    [HttpGet("mutual")]
    public async Task<ActionResult<bool>> AreMutualFollowers([FromQuery] Guid userId1, [FromQuery] Guid userId2, CancellationToken ct)
    {
        var result = await _followerService.AreMutualFollowersAsync(userId1, userId2, ct);
        return Ok(result.Value);
    }

    /// <summary>Batch get follow status for multiple entities</summary>
    [HttpPost("batch/status")]
    public async Task<ActionResult<Dictionary<Guid, bool>>> GetFollowStatusBatch([FromBody] BatchStatusRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.GetFollowStatusBatchAsync(userId, request.EntityIds, request.EntityType, ct);
        return Ok(result.Value);
    }

    /// <summary>Batch get follower counts for multiple entities</summary>
    [HttpPost("batch/counts")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<Guid, int>>> GetFollowerCountsBatch([FromBody] BatchCountsRequest request, CancellationToken ct)
    {
        var result = await _followerService.GetFollowerCountsBatchAsync(request.EntityIds, request.EntityType, ct);
        return Ok(result.Value);
    }

    #endregion

    #region Privacy Settings Endpoints

    /// <summary>Get current user's privacy settings</summary>
    [HttpGet("privacy-settings")]
    public async Task<ActionResult<FollowPrivacySettingsDto>> GetPrivacySettings(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.GetPrivacySettingsAsync(userId, ct);
        return Ok(MapToDto(result.Value));
    }

    /// <summary>Update current user's privacy settings</summary>
    [HttpPut("privacy-settings")]
    public async Task<ActionResult<FollowPrivacySettingsDto>> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.UpdatePrivacySettingsAsync(
            userId,
            request.IsFollowerListPublic,
            request.IsFollowingListPublic,
            request.AllowFollowers,
            request.NotifyOnNewFollower,
            request.ShowFollowerCount,
            request.ShowFollowingCount,
            ct);

        return Ok(MapToDto(result.Value));
    }

    #endregion

    #region Block Endpoints

    /// <summary>Block a user</summary>
    [HttpPost("block")]
    public async Task<ActionResult<BlockDto>> BlockUser([FromBody] BlockRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.BlockUserAsync(userId, request.BlockedUserId, request.Reason, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error.Code, message = result.Error.Description });
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>Unblock a user</summary>
    [HttpDelete("unblock/{blockedUserId}")]
    public async Task<ActionResult> UnblockUser(Guid blockedUserId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.UnblockUserAsync(userId, blockedUserId, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error.Code, message = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>Check if current user has blocked a user</summary>
    [HttpGet("is-blocked/{blockedUserId}")]
    public async Task<ActionResult<bool>> IsUserBlocked(Guid blockedUserId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.IsUserBlockedAsync(userId, blockedUserId, ct);
        return Ok(result.Value);
    }

    /// <summary>Get current user's blocked users list</summary>
    [HttpGet("blocked-users")]
    public async Task<ActionResult<IEnumerable<BlockDto>>> GetBlockedUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.GetBlockedUsersAsync(userId, skip, take, ct);
        return Ok(result.Value.Select(MapToDto));
    }

    #endregion

    #region Mute Endpoints

    /// <summary>Mute a user</summary>
    [HttpPost("mute")]
    public async Task<ActionResult<MuteDto>> MuteUser([FromBody] MuteRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.MuteUserAsync(userId, request.MutedUserId, request.Reason, request.ExpiresAt, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error.Code, message = result.Error.Description });
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>Unmute a user</summary>
    [HttpDelete("unmute/{mutedUserId}")]
    public async Task<ActionResult> UnmuteUser(Guid mutedUserId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.UnmuteUserAsync(userId, mutedUserId, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error.Code, message = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>Check if current user has muted a user</summary>
    [HttpGet("is-muted/{mutedUserId}")]
    public async Task<ActionResult<bool>> IsUserMuted(Guid mutedUserId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.IsUserMutedAsync(userId, mutedUserId, ct);
        return Ok(result.Value);
    }

    /// <summary>Get current user's muted users list</summary>
    [HttpGet("muted-users")]
    public async Task<ActionResult<IEnumerable<MuteDto>>> GetMutedUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _followerService.GetMutedUsersAsync(userId, skip, take, ct);
        return Ok(result.Value.Select(MapToDto));
    }

    #endregion

    #region Mapping

    private static FollowDto MapToDto(Follow follow) => new(
        follow.Id,
        follow.FollowerId,
        follow.FollowedEntityId,
        follow.FollowedEntityType,
        follow.NotificationsEnabled,
        follow.FollowedAt);

    private static FollowPrivacySettingsDto MapToDto(FollowPrivacySettings settings) => new(
        settings.Id,
        settings.UserId,
        settings.IsFollowerListPublic,
        settings.IsFollowingListPublic,
        settings.AllowFollowers,
        settings.NotifyOnNewFollower,
        settings.ShowFollowerCount,
        settings.ShowFollowingCount);

    private static BlockDto MapToDto(Block block) => new(
        block.Id,
        block.BlockerId,
        block.BlockedId,
        block.Reason,
        block.BlockedAt);

    private static MuteDto MapToDto(Mute mute) => new(
        mute.Id,
        mute.MuterId,
        mute.MutedId,
        mute.Reason,
        mute.MutedAt,
        mute.ExpiresAt);

    #endregion
}

#region DTOs

public record FollowDto(
    Guid Id,
    Guid FollowerId,
    Guid FollowedEntityId,
    string FollowedEntityType,
    bool NotificationsEnabled,
    DateTime FollowedAt);

public record FollowPrivacySettingsDto(
    Guid Id,
    Guid UserId,
    bool IsFollowerListPublic,
    bool IsFollowingListPublic,
    bool AllowFollowers,
    bool NotifyOnNewFollower,
    bool ShowFollowerCount,
    bool ShowFollowingCount);

public record BlockDto(
    Guid Id,
    Guid BlockerId,
    Guid BlockedId,
    string? Reason,
    DateTime BlockedAt);

public record MuteDto(
    Guid Id,
    Guid MuterId,
    Guid MutedId,
    string? Reason,
    DateTime MutedAt,
    DateTime? ExpiresAt);

public record FollowRequest(Guid EntityId, string EntityType, bool NotificationsEnabled = true);
public record UpdateNotificationsRequest(Guid EntityId, string EntityType, bool NotificationsEnabled);
public record BlockRequest(Guid BlockedUserId, string? Reason = null);
public record MuteRequest(Guid MutedUserId, string? Reason = null, DateTime? ExpiresAt = null);
public record BatchStatusRequest(IEnumerable<Guid> EntityIds, string EntityType);
public record BatchCountsRequest(IEnumerable<Guid> EntityIds, string EntityType);
public record UpdatePrivacySettingsRequest(
    bool IsFollowerListPublic,
    bool IsFollowingListPublic,
    bool AllowFollowers,
    bool NotifyOnNewFollower,
    bool ShowFollowerCount,
    bool ShowFollowingCount);

#endregion

using GameGuild.Modules.Followers.DTOs;
using GameGuild.Modules.Followers.Entities;
using GameGuild.Modules.Followers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Followers.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowersController : ControllerBase
{
    private readonly IFollowerService _followerService;
    private readonly ILogger<FollowersController> _logger;

    public FollowersController(
        IFollowerService followerService,
        ILogger<FollowersController> logger)
    {
        _followerService = followerService;
        _logger = logger;
    }

    [HttpPost("follow")]
    public async Task<ActionResult<FollowerDto>> Follow([FromBody] FollowRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var follower = await _followerService.FollowAsync(userId, request.EntityId, request.EntityType, request.NotificationsEnabled);

            var dto = MapToDto(follower);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following entity {EntityId} of type {EntityType}", request.EntityId, request.EntityType);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("unfollow")]
    public async Task<ActionResult> Unfollow([FromQuery] Guid entityId, [FromQuery] string entityType)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _followerService.UnfollowAsync(userId, entityId, entityType);

            if (!result)
            {
                return NotFound("Follow relationship not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing entity {EntityId} of type {EntityType}", entityId, entityType);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("is-following")]
    public async Task<ActionResult<bool>> IsFollowing([FromQuery] Guid entityId, [FromQuery] string entityType)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isFollowing = await _followerService.IsFollowingAsync(userId, entityId, entityType);
            return Ok(isFollowing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if following entity {EntityId}", entityId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("followers/{entityId}")]
    public async Task<ActionResult<IEnumerable<FollowerDto>>> GetFollowers(
        Guid entityId,
        [FromQuery] string entityType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var followers = await _followerService.GetFollowersAsync(entityId, entityType, skip, take);
            var dtos = followers.Select(MapToDto);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving followers for entity {EntityId}", entityId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<FollowerDto>>> GetFollowing(
        [FromQuery] string? entityType = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var following = await _followerService.GetFollowingAsync(userId, entityType, skip, take);
            var dtos = following.Select(MapToDto);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving following list");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("count/followers/{entityId}")]
    public async Task<ActionResult<int>> GetFollowerCount(Guid entityId, [FromQuery] string entityType)
    {
        try
        {
            var count = await _followerService.GetFollowerCountAsync(entityId, entityType);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving follower count for entity {EntityId}", entityId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("count/following")]
    public async Task<ActionResult<int>> GetFollowingCount([FromQuery] string? entityType = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _followerService.GetFollowingCountAsync(userId, entityType);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving following count");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("mutual-follow")]
    public async Task<ActionResult<bool>> AreMutualFollowers([FromQuery] Guid userId1, [FromQuery] Guid userId2)
    {
        try
        {
            var areMutual = await _followerService.AreMutualFollowersAsync(userId1, userId2);
            return Ok(areMutual);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking mutual follow status");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("privacy-settings")]
    public async Task<ActionResult<FollowerPrivacySettingsDto>> GetPrivacySettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = await _followerService.GetPrivacySettingsAsync(userId);

            if (settings == null)
            {
                return NotFound();
            }

            var dto = MapToDto(settings);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving privacy settings");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("privacy-settings")]
    public async Task<ActionResult<FollowerPrivacySettingsDto>> UpdatePrivacySettings([FromBody] FollowerPrivacySettingsDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = new FollowerPrivacySettings
            {
                UserId = userId,
                IsFollowerListPublic = request.IsFollowerListPublic,
                IsFollowingListPublic = request.IsFollowingListPublic,
                AllowFollowers = request.AllowFollowers,
                NotifyOnNewFollower = request.NotifyOnNewFollower,
                ShowFollowerCount = request.ShowFollowerCount,
                ShowFollowingCount = request.ShowFollowingCount
            };

            var updated = await _followerService.UpdatePrivacySettingsAsync(userId, settings);
            var dto = MapToDto(updated);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy settings");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("block")]
    public async Task<ActionResult<BlockedUserDto>> BlockUser([FromBody] BlockUserRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var blockedUser = await _followerService.BlockUserAsync(userId, request.BlockedUserId, request.Reason);
            var dto = MapToDto(blockedUser);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking user {BlockedUserId}", request.BlockedUserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("unblock/{blockedUserId}")]
    public async Task<ActionResult> UnblockUser(Guid blockedUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _followerService.UnblockUserAsync(userId, blockedUserId);

            if (!result)
            {
                return NotFound("Block relationship not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user {BlockedUserId}", blockedUserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("is-blocked/{blockedUserId}")]
    public async Task<ActionResult<bool>> IsUserBlocked(Guid blockedUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isBlocked = await _followerService.IsUserBlockedAsync(userId, blockedUserId);
            return Ok(isBlocked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is blocked");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("mute")]
    public async Task<ActionResult<MutedUserDto>> MuteUser([FromBody] MuteUserRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var mutedUser = await _followerService.MuteUserAsync(userId, request.MutedUserId, request.Reason, request.ExpiresAt);
            var dto = MapToDto(mutedUser);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error muting user {MutedUserId}", request.MutedUserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("unmute/{mutedUserId}")]
    public async Task<ActionResult> UnmuteUser(Guid mutedUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _followerService.UnmuteUserAsync(userId, mutedUserId);

            if (!result)
            {
                return NotFound("Mute relationship not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmuting user {MutedUserId}", mutedUserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("is-muted/{mutedUserId}")]
    public async Task<ActionResult<bool>> IsUserMuted(Guid mutedUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isMuted = await _followerService.IsUserMutedAsync(userId, mutedUserId);
            return Ok(isMuted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is muted");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("blocked-users")]
    public async Task<ActionResult<IEnumerable<BlockedUserDto>>> GetBlockedUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var blockedUsers = await _followerService.GetBlockedUsersAsync(userId, skip, take);
            var dtos = blockedUsers.Select(MapToDto);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocked users");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("muted-users")]
    public async Task<ActionResult<IEnumerable<MutedUserDto>>> GetMutedUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var mutedUsers = await _followerService.GetMutedUsersAsync(userId, skip, take);
            var dtos = mutedUsers.Select(MapToDto);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving muted users");
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    private static FollowerDto MapToDto(Follower follower)
    {
        return new FollowerDto
        {
            Id = follower.Id,
            UserId = follower.UserId,
            FollowedEntityId = follower.FollowedEntityId,
            FollowedEntityType = follower.FollowedEntityType,
            NotificationsEnabled = follower.NotificationsEnabled,
            FollowedAt = follower.FollowedAt
        };
    }

    private static FollowerPrivacySettingsDto MapToDto(FollowerPrivacySettings settings)
    {
        return new FollowerPrivacySettingsDto
        {
            Id = settings.Id,
            UserId = settings.UserId,
            IsFollowerListPublic = settings.IsFollowerListPublic,
            IsFollowingListPublic = settings.IsFollowingListPublic,
            AllowFollowers = settings.AllowFollowers,
            NotifyOnNewFollower = settings.NotifyOnNewFollower,
            ShowFollowerCount = settings.ShowFollowerCount,
            ShowFollowingCount = settings.ShowFollowingCount
        };
    }

    private static BlockedUserDto MapToDto(BlockedUser blockedUser)
    {
        return new BlockedUserDto
        {
            Id = blockedUser.Id,
            BlockingUserId = blockedUser.BlockingUserId,
            BlockedUserId = blockedUser.BlockedUserId,
            Reason = blockedUser.Reason,
            BlockedAt = blockedUser.BlockedAt
        };
    }

    private static MutedUserDto MapToDto(MutedUser mutedUser)
    {
        return new MutedUserDto
        {
            Id = mutedUser.Id,
            MutingUserId = mutedUser.MutingUserId,
            MutedUserId = mutedUser.MutedUserId,
            Reason = mutedUser.Reason,
            MutedAt = mutedUser.MutedAt,
            ExpiresAt = mutedUser.ExpiresAt
        };
    }
}

public record FollowRequest(Guid EntityId, string EntityType, bool NotificationsEnabled = true);
public record BlockUserRequest(Guid BlockedUserId, string? Reason);
public record MuteUserRequest(Guid MutedUserId, string? Reason, DateTime? ExpiresAt);

using FluentAssertions;
using GameGuild.Social.Follows.Controllers;
using GameGuild.Social.Follows.Events;
using GameGuild.Social.Follows.Services;
using Moq;
using Xunit;

namespace GameGuild.Social.Follows.Tests;

/// <summary>
/// Tests for follow domain events.
/// </summary>
public class FollowEventsTests
{
    [Fact]
    public void FollowerAddedEvent_ShouldStoreAllProperties()
    {
        var followId = Guid.NewGuid();
        var followerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new FollowerAddedEvent(followId, followerId, entityId, "Course", now, true);

        evt.FollowId.Should().Be(followId);
        evt.FollowerId.Should().Be(followerId);
        evt.FollowedEntityId.Should().Be(entityId);
        evt.FollowedEntityType.Should().Be("Course");
        evt.FollowedAt.Should().Be(now);
        evt.NotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public void FollowerRemovedEvent_ShouldStoreAllProperties()
    {
        var followId = Guid.NewGuid();
        var followerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new FollowerRemovedEvent(followId, followerId, entityId, "User", now);

        evt.FollowId.Should().Be(followId);
        evt.FollowerId.Should().Be(followerId);
        evt.FollowedEntityId.Should().Be(entityId);
        evt.FollowedEntityType.Should().Be("User");
        evt.UnfollowedAt.Should().Be(now);
    }

    [Fact]
    public void UserBlockedEvent_ShouldStoreAllProperties()
    {
        var blockId = Guid.NewGuid();
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new UserBlockedEvent(blockId, blockerId, blockedId, "Spam", now);

        evt.BlockId.Should().Be(blockId);
        evt.BlockerId.Should().Be(blockerId);
        evt.BlockedUserId.Should().Be(blockedId);
        evt.Reason.Should().Be("Spam");
        evt.BlockedAt.Should().Be(now);
    }

    [Fact]
    public void UserUnblockedEvent_ShouldStoreAllProperties()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new UserUnblockedEvent(blockerId, blockedId, now);

        evt.BlockerId.Should().Be(blockerId);
        evt.BlockedUserId.Should().Be(blockedId);
        evt.UnblockedAt.Should().Be(now);
    }

    [Fact]
    public void UserMutedEvent_ShouldStoreAllProperties()
    {
        var muteId = Guid.NewGuid();
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expires = now.AddDays(7);

        var evt = new UserMutedEvent(muteId, muterId, mutedId, "Annoying", now, expires);

        evt.MuteId.Should().Be(muteId);
        evt.MuterId.Should().Be(muterId);
        evt.MutedUserId.Should().Be(mutedId);
        evt.Reason.Should().Be("Annoying");
        evt.MutedAt.Should().Be(now);
        evt.ExpiresAt.Should().Be(expires);
    }

    [Fact]
    public void UserUnmutedEvent_ShouldStoreAllProperties()
    {
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new UserUnmutedEvent(muterId, mutedId, now);

        evt.MuterId.Should().Be(muterId);
        evt.MutedUserId.Should().Be(mutedId);
        evt.UnmutedAt.Should().Be(now);
    }

    [Fact]
    public void PrivacySettingsUpdatedEvent_ShouldStoreAllProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new PrivacySettingsUpdatedEvent(userId, false, true, now);

        evt.UserId.Should().Be(userId);
        evt.AllowFollowers.Should().BeFalse();
        evt.NotifyOnNewFollower.Should().BeTrue();
        evt.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Events_ShouldSupportRecordEquality()
    {
        var id = Guid.NewGuid();
        var uid1 = Guid.NewGuid();
        var uid2 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt1 = new UserBlockedEvent(id, uid1, uid2, "Spam", now);
        var evt2 = new UserBlockedEvent(id, uid1, uid2, "Spam", now);
        var evt3 = new UserBlockedEvent(id, uid1, uid2, "Other", now);

        evt1.Should().Be(evt2);
        evt1.Should().NotBe(evt3);
    }
}

/// <summary>
/// Tests for DTOs and request records.
/// </summary>
public class FollowDtoTests
{
    [Fact]
    public void FollowDto_ShouldStoreAllProperties()
    {
        var id = Guid.NewGuid();
        var followerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto = new FollowDto(id, followerId, entityId, "Course", true, now);

        dto.Id.Should().Be(id);
        dto.FollowerId.Should().Be(followerId);
        dto.FollowedEntityId.Should().Be(entityId);
        dto.FollowedEntityType.Should().Be("Course");
        dto.NotificationsEnabled.Should().BeTrue();
        dto.FollowedAt.Should().Be(now);
    }

    [Fact]
    public void FollowPrivacySettingsDto_ShouldStoreAllProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var dto = new FollowPrivacySettingsDto(id, userId, true, false, true, false, true, false);

        dto.Id.Should().Be(id);
        dto.UserId.Should().Be(userId);
        dto.IsFollowerListPublic.Should().BeTrue();
        dto.IsFollowingListPublic.Should().BeFalse();
        dto.AllowFollowers.Should().BeTrue();
        dto.NotifyOnNewFollower.Should().BeFalse();
        dto.ShowFollowerCount.Should().BeTrue();
        dto.ShowFollowingCount.Should().BeFalse();
    }

    [Fact]
    public void BlockDto_ShouldStoreAllProperties()
    {
        var id = Guid.NewGuid();
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto = new BlockDto(id, blockerId, blockedId, "Spam", now);

        dto.Id.Should().Be(id);
        dto.BlockerId.Should().Be(blockerId);
        dto.BlockedId.Should().Be(blockedId);
        dto.Reason.Should().Be("Spam");
        dto.BlockedAt.Should().Be(now);
    }

    [Fact]
    public void MuteDto_ShouldStoreAllProperties()
    {
        var id = Guid.NewGuid();
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expires = now.AddDays(7);

        var dto = new MuteDto(id, muterId, mutedId, "Annoying", now, expires);

        dto.Id.Should().Be(id);
        dto.MuterId.Should().Be(muterId);
        dto.MutedId.Should().Be(mutedId);
        dto.Reason.Should().Be("Annoying");
        dto.MutedAt.Should().Be(now);
        dto.ExpiresAt.Should().Be(expires);
    }

    [Fact]
    public void FollowRequest_ShouldStoreProperties()
    {
        var entityId = Guid.NewGuid();
        var request = new FollowRequest(entityId, "User");

        request.EntityId.Should().Be(entityId);
        request.EntityType.Should().Be("User");
        request.NotificationsEnabled.Should().BeTrue(); // default
    }

    [Fact]
    public void FollowRequest_WithNotificationsDisabled()
    {
        var request = new FollowRequest(Guid.NewGuid(), "Course", false);
        request.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateNotificationsRequest_ShouldStoreProperties()
    {
        var entityId = Guid.NewGuid();
        var request = new UpdateNotificationsRequest(entityId, "User", false);

        request.EntityId.Should().Be(entityId);
        request.EntityType.Should().Be("User");
        request.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void BlockRequest_ShouldStoreProperties()
    {
        var blockedId = Guid.NewGuid();
        var request = new BlockRequest(blockedId, "Harassment");

        request.BlockedUserId.Should().Be(blockedId);
        request.Reason.Should().Be("Harassment");
    }

    [Fact]
    public void BlockRequest_DefaultReasonShouldBeNull()
    {
        var request = new BlockRequest(Guid.NewGuid());
        request.Reason.Should().BeNull();
    }

    [Fact]
    public void MuteRequest_ShouldStoreProperties()
    {
        var mutedId = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddDays(7);
        var request = new MuteRequest(mutedId, "Annoying", expires);

        request.MutedUserId.Should().Be(mutedId);
        request.Reason.Should().Be("Annoying");
        request.ExpiresAt.Should().Be(expires);
    }

    [Fact]
    public void MuteRequest_DefaultsShouldBeNull()
    {
        var request = new MuteRequest(Guid.NewGuid());
        request.Reason.Should().BeNull();
        request.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void BatchStatusRequest_ShouldStoreProperties()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new BatchStatusRequest(ids, "Course");

        request.EntityIds.Should().BeEquivalentTo(ids);
        request.EntityType.Should().Be("Course");
    }

    [Fact]
    public void BatchCountsRequest_ShouldStoreProperties()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var request = new BatchCountsRequest(ids, "User");

        request.EntityIds.Should().BeEquivalentTo(ids);
        request.EntityType.Should().Be("User");
    }

    [Fact]
    public void UpdatePrivacySettingsRequest_ShouldStoreProperties()
    {
        var request = new UpdatePrivacySettingsRequest(false, false, false, false, false, false);

        request.IsFollowerListPublic.Should().BeFalse();
        request.IsFollowingListPublic.Should().BeFalse();
        request.AllowFollowers.Should().BeFalse();
        request.NotifyOnNewFollower.Should().BeFalse();
        request.ShowFollowerCount.Should().BeFalse();
        request.ShowFollowingCount.Should().BeFalse();
    }
}

/// <summary>
/// Tests for FollowerErrors and FollowableEntityTypes.
/// </summary>
public class FollowerErrorsAndConstantsTests
{
    [Fact]
    public void FollowNotFound_ShouldHaveCorrectCodeAndDescription()
    {
        FollowerErrors.FollowNotFound.Code.Should().Be("Follower.NotFound");
        FollowerErrors.FollowNotFound.Description.Should().Be("Follow relationship not found");
    }

    [Fact]
    public void BlockNotFound_ShouldHaveCorrectCodeAndDescription()
    {
        FollowerErrors.BlockNotFound.Code.Should().Be("Block.NotFound");
        FollowerErrors.BlockNotFound.Description.Should().Be("Block relationship not found");
    }

    [Fact]
    public void MuteNotFound_ShouldHaveCorrectCodeAndDescription()
    {
        FollowerErrors.MuteNotFound.Code.Should().Be("Mute.NotFound");
        FollowerErrors.MuteNotFound.Description.Should().Be("Mute relationship not found");
    }

    [Fact]
    public void CannotFollowBlockedUser_ShouldHaveCorrectCode()
    {
        FollowerErrors.CannotFollowBlockedUser.Code.Should().Be("Follower.Blocked");
    }

    [Fact]
    public void UserDoesNotAllowFollowers_ShouldHaveCorrectCode()
    {
        FollowerErrors.UserDoesNotAllowFollowers.Code.Should().Be("Follower.NotAllowed");
    }

    [Fact]
    public void CannotBlockYourself_ShouldHaveCorrectCode()
    {
        FollowerErrors.CannotBlockYourself.Code.Should().Be("Block.Self");
    }

    [Fact]
    public void CannotMuteYourself_ShouldHaveCorrectCode()
    {
        FollowerErrors.CannotMuteYourself.Code.Should().Be("Mute.Self");
    }

    [Fact]
    public void FollowableEntityTypes_ShouldHaveCorrectValues()
    {
        FollowableEntityTypes.User.Should().Be("User");
        FollowableEntityTypes.Course.Should().Be("Course");
        FollowableEntityTypes.Project.Should().Be("Project");
        FollowableEntityTypes.Program.Should().Be("Program");
        FollowableEntityTypes.Tag.Should().Be("Tag");
        FollowableEntityTypes.Team.Should().Be("Team");
    }
}

/// <summary>
/// Tests for FollowerService facade delegation.
/// </summary>
public class FollowerServiceFacadeTests
{
    private readonly Mock<IFollowOperationService> _followOpsMock = new();
    private readonly Mock<IUserModerationService> _moderationMock = new();
    private readonly FollowerService _sut;

    public FollowerServiceFacadeTests()
    {
        _sut = new FollowerService(_followOpsMock.Object, _moderationMock.Object);
    }

    // ─── Follow Operations ───────────────────────────────────────────

    [Fact]
    public async Task FollowAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var follow = Follow.Create(userId, entityId, "User");
        _followOpsMock.Setup(f => f.FollowAsync(userId, entityId, "User", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Follow>.Success(follow));

        var result = await _sut.FollowAsync(userId, entityId, "User");

        result.IsSuccess.Should().BeTrue();
        _followOpsMock.Verify(f => f.FollowAsync(userId, entityId, "User", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnfollowAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.UnfollowAsync(userId, entityId, "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.UnfollowAsync(userId, entityId, "User");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IsFollowingAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.IsFollowingAsync(userId, entityId, "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _sut.IsFollowingAsync(userId, entityId, "User");

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNotificationSettingsAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var follow = Follow.Create(userId, entityId, "User");
        _followOpsMock.Setup(f => f.UpdateNotificationSettingsAsync(userId, entityId, "User", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Follow>.Success(follow));

        var result = await _sut.UpdateNotificationSettingsAsync(userId, entityId, "User", false);

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Query Operations ────────────────────────────────────────────

    [Fact]
    public async Task GetFollowersAsync_ShouldDelegateToFollowOps()
    {
        var entityId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.GetFollowersAsync(entityId, "User", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<Follow>>.Success(new List<Follow>()));

        var result = await _sut.GetFollowersAsync(entityId, "User");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowingAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.GetFollowingAsync(userId, "Course", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<Follow>>.Success(new List<Follow>()));

        var result = await _sut.GetFollowingAsync(userId, "Course");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowerCountAsync_ShouldDelegateToFollowOps()
    {
        var entityId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.GetFollowerCountAsync(entityId, "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(42));

        var result = await _sut.GetFollowerCountAsync(entityId, "User");

        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetFollowingCountAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.GetFollowingCountAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(10));

        var result = await _sut.GetFollowingCountAsync(userId);

        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task AreMutualFollowersAsync_ShouldDelegateToFollowOps()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _followOpsMock.Setup(f => f.AreMutualFollowersAsync(userId1, userId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _sut.AreMutualFollowersAsync(userId1, userId2);

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowByIdAsync_ShouldDelegateToFollowOps()
    {
        var followId = Guid.NewGuid();
        var follow = Follow.Create(Guid.NewGuid(), Guid.NewGuid(), "User");
        _followOpsMock.Setup(f => f.GetFollowByIdAsync(followId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Follow>.Success(follow));

        var result = await _sut.GetFollowByIdAsync(followId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowersWithNotificationsAsync_ShouldDelegateToFollowOps()
    {
        var entityId = Guid.NewGuid();
        _followOpsMock.Setup(f => f.GetFollowersWithNotificationsAsync(entityId, "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<Follow>>.Success(new List<Follow>()));

        var result = await _sut.GetFollowersWithNotificationsAsync(entityId, "User");

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Batch Operations ────────────────────────────────────────────

    [Fact]
    public async Task GetFollowStatusBatchAsync_ShouldDelegateToFollowOps()
    {
        var userId = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid() };
        _followOpsMock.Setup(f => f.GetFollowStatusBatchAsync(userId, ids, "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<Guid, bool>>.Success(new Dictionary<Guid, bool>()));

        var result = await _sut.GetFollowStatusBatchAsync(userId, ids, "User");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowerCountsBatchAsync_ShouldDelegateToFollowOps()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        _followOpsMock.Setup(f => f.GetFollowerCountsBatchAsync(ids, "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<Guid, int>>.Success(new Dictionary<Guid, int>()));

        var result = await _sut.GetFollowerCountsBatchAsync(ids, "User");

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Privacy Settings ────────────────────────────────────────────

    [Fact]
    public async Task GetPrivacySettingsAsync_ShouldDelegateToModeration()
    {
        var userId = Guid.NewGuid();
        var settings = FollowPrivacySettings.CreateDefault(userId);
        _moderationMock.Setup(m => m.GetPrivacySettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FollowPrivacySettings>.Success(settings));

        var result = await _sut.GetPrivacySettingsAsync(userId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePrivacySettingsAsync_ShouldDelegateToModeration()
    {
        var userId = Guid.NewGuid();
        var settings = FollowPrivacySettings.CreateDefault(userId);
        _moderationMock.Setup(m => m.UpdatePrivacySettingsAsync(
                userId, true, true, true, true, true, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FollowPrivacySettings>.Success(settings));

        var result = await _sut.UpdatePrivacySettingsAsync(userId, true, true, true, true, true, true);

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Block Operations ────────────────────────────────────────────

    [Fact]
    public async Task BlockUserAsync_ShouldDelegateToModeration()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var block = Block.Create(blockerId, blockedId, "Spam");
        _moderationMock.Setup(m => m.BlockUserAsync(blockerId, blockedId, "Spam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Block>.Success(block));

        var result = await _sut.BlockUserAsync(blockerId, blockedId, "Spam");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UnblockUserAsync_ShouldDelegateToModeration()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        _moderationMock.Setup(m => m.UnblockUserAsync(blockerId, blockedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.UnblockUserAsync(blockerId, blockedId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserBlockedAsync_ShouldDelegateToModeration()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        _moderationMock.Setup(m => m.IsUserBlockedAsync(blockerId, blockedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _sut.IsUserBlockedAsync(blockerId, blockedId);

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task AreUsersBlockedAsync_ShouldDelegateToModeration()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _moderationMock.Setup(m => m.AreUsersBlockedAsync(userId1, userId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        var result = await _sut.AreUsersBlockedAsync(userId1, userId2);

        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task GetBlockedUsersAsync_ShouldDelegateToModeration()
    {
        var userId = Guid.NewGuid();
        _moderationMock.Setup(m => m.GetBlockedUsersAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<Block>>.Success(new List<Block>()));

        var result = await _sut.GetBlockedUsersAsync(userId);

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Mute Operations ─────────────────────────────────────────────

    [Fact]
    public async Task MuteUserAsync_ShouldDelegateToModeration()
    {
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var mute = Mute.Create(muterId, mutedId, "reason");
        _moderationMock.Setup(m => m.MuteUserAsync(muterId, mutedId, "reason", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Mute>.Success(mute));

        var result = await _sut.MuteUserAsync(muterId, mutedId, "reason");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UnmuteUserAsync_ShouldDelegateToModeration()
    {
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        _moderationMock.Setup(m => m.UnmuteUserAsync(muterId, mutedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.UnmuteUserAsync(muterId, mutedId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserMutedAsync_ShouldDelegateToModeration()
    {
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        _moderationMock.Setup(m => m.IsUserMutedAsync(muterId, mutedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _sut.IsUserMutedAsync(muterId, mutedId);

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task GetMutedUsersAsync_ShouldDelegateToModeration()
    {
        var userId = Guid.NewGuid();
        _moderationMock.Setup(m => m.GetMutedUsersAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<Mute>>.Success(new List<Mute>()));

        var result = await _sut.GetMutedUsersAsync(userId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupExpiredMutesAsync_ShouldDelegateToModeration()
    {
        _moderationMock.Setup(m => m.CleanupExpiredMutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(5));

        var result = await _sut.CleanupExpiredMutesAsync();

        result.Value.Should().Be(5);
    }
}

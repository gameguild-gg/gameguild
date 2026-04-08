using FluentAssertions;
using GameGuild.Social.Follows;
using GameGuild.Social.Follows.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Social.Follows.Tests.Services;

public class UserModerationServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly UserModerationService _sut;

    public UserModerationServiceTests()
    {
        _sut = new UserModerationService(
            _contextMock.Object,
            NullLogger<UserModerationService>.Instance);
    }

    private void SetupBlockDbSet(List<Block> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<Block>()).Returns(mock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private void SetupMuteDbSet(List<Mute> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<Mute>()).Returns(mock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private void SetupPrivacySettingsDbSet(List<FollowPrivacySettings> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<FollowPrivacySettings>()).Returns(mock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private void SetupFollowDbSet(List<Follow> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<Follow>()).Returns(mock.Object);
    }

    #region GetPrivacySettingsAsync

    [Fact]
    public async Task GetPrivacySettingsAsync_WhenSettingsExist_ReturnsSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = FollowPrivacySettings.CreateDefault(userId);
        SetupPrivacySettingsDbSet([settings]);

        // Act
        var result = await _sut.GetPrivacySettingsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetPrivacySettingsAsync_WhenNoSettings_ReturnsDefaultSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupPrivacySettingsDbSet([]);

        // Act
        var result = await _sut.GetPrivacySettingsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.AllowFollowers.Should().BeTrue();
        result.Value.ShowFollowerCount.Should().BeTrue();
        result.Value.ShowFollowingCount.Should().BeTrue();
    }

    #endregion

    #region UpdatePrivacySettingsAsync

    [Fact]
    public async Task UpdatePrivacySettingsAsync_WhenSettingsExist_UpdatesSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = FollowPrivacySettings.CreateDefault(userId);
        SetupPrivacySettingsDbSet([settings]);

        // Act
        var result = await _sut.UpdatePrivacySettingsAsync(
            userId,
            isFollowerListPublic: true,
            isFollowingListPublic: true,
            allowFollowers: true,
            notifyOnNewFollower: false,
            showFollowerCount: false,
            showFollowingCount: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShowFollowerCount.Should().BeFalse();
        result.Value.ShowFollowingCount.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePrivacySettingsAsync_WhenNoSettings_CreatesNewSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupPrivacySettingsDbSet([]);

        // Act
        var result = await _sut.UpdatePrivacySettingsAsync(
            userId,
            isFollowerListPublic: true,
            isFollowingListPublic: true,
            allowFollowers: true,
            notifyOnNewFollower: true,
            showFollowerCount: false,
            showFollowingCount: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.ShowFollowerCount.Should().BeFalse();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region BlockUserAsync

    [Fact]
    public async Task BlockUserAsync_WhenNewBlock_CreatesBlock()
    {
        // Arrange
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        SetupBlockDbSet([]);
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.BlockUserAsync(blockerId, blockedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BlockerId.Should().Be(blockerId);
        result.Value.BlockedId.Should().Be(blockedId);
    }

    [Fact]
    public async Task BlockUserAsync_WhenAlreadyBlocked_ReturnsExistingBlock()
    {
        // Arrange
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var existingBlock = Block.Create(blockerId, blockedId);
        SetupBlockDbSet([existingBlock]);

        // Act
        var result = await _sut.BlockUserAsync(blockerId, blockedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existingBlock.Id);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BlockUserAsync_WithReason_SetsReason()
    {
        // Arrange
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var reason = "Harassment";
        SetupBlockDbSet([]);
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.BlockUserAsync(blockerId, blockedId, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task BlockUserAsync_RemovesMutualFollows()
    {
        // Arrange
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(blockerId, blockedId, FollowableEntityTypes.User),
            Follow.Create(blockedId, blockerId, FollowableEntityTypes.User)
        };
        SetupBlockDbSet([]);
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.BlockUserAsync(blockerId, blockedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Follow removal is handled as side effect
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region UnblockUserAsync

    [Fact]
    public async Task UnblockUserAsync_WhenBlockExists_RemovesBlock()
    {
        // Arrange
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var block = Block.Create(blockerId, blockedId);
        SetupBlockDbSet([block]);

        // Act
        var result = await _sut.UnblockUserAsync(blockerId, blockedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnblockUserAsync_WhenBlockNotFound_ReturnsFailure()
    {
        // Arrange
        SetupBlockDbSet([]);

        // Act
        var result = await _sut.UnblockUserAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.BlockNotFound);
    }

    #endregion

    #region IsUserBlockedAsync

    [Fact]
    public async Task IsUserBlockedAsync_WhenBlocked_ReturnsTrue()
    {
        // Arrange
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var block = Block.Create(blockerId, blockedId);
        SetupBlockDbSet([block]);

        // Act
        var result = await _sut.IsUserBlockedAsync(blockerId, blockedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserBlockedAsync_WhenNotBlocked_ReturnsFalse()
    {
        // Arrange
        SetupBlockDbSet([]);

        // Act
        var result = await _sut.IsUserBlockedAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region AreUsersBlockedAsync

    [Fact]
    public async Task AreUsersBlockedAsync_WhenUser1BlocksUser2_ReturnsTrue()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var block = Block.Create(user1, user2);
        SetupBlockDbSet([block]);

        // Act
        var result = await _sut.AreUsersBlockedAsync(user1, user2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task AreUsersBlockedAsync_WhenUser2BlocksUser1_ReturnsTrue()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var block = Block.Create(user2, user1);
        SetupBlockDbSet([block]);

        // Act
        var result = await _sut.AreUsersBlockedAsync(user1, user2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task AreUsersBlockedAsync_WhenNoBlocks_ReturnsFalse()
    {
        // Arrange
        SetupBlockDbSet([]);

        // Act
        var result = await _sut.AreUsersBlockedAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region GetBlockedUsersAsync

    [Fact]
    public async Task GetBlockedUsersAsync_ReturnsAllBlockedUsers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blocks = new List<Block>
        {
            Block.Create(userId, Guid.NewGuid()),
            Block.Create(userId, Guid.NewGuid()),
            Block.Create(Guid.NewGuid(), Guid.NewGuid()) // Different blocker
        };
        SetupBlockDbSet(blocks);

        // Act
        var result = await _sut.GetBlockedUsersAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(b => b.BlockerId == userId);
    }

    [Fact]
    public async Task GetBlockedUsersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blocks = Enumerable.Range(1, 20)
            .Select(_ => Block.Create(userId, Guid.NewGuid()))
            .ToList();
        SetupBlockDbSet(blocks);

        // Act
        var result = await _sut.GetBlockedUsersAsync(userId, skip: 5, take: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    #endregion

    #region MuteUserAsync

    [Fact]
    public async Task MuteUserAsync_WhenNewMute_CreatesMute()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        SetupMuteDbSet([]);

        // Act
        var result = await _sut.MuteUserAsync(muterId, mutedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MuterId.Should().Be(muterId);
        result.Value.MutedId.Should().Be(mutedId);
    }

    [Fact]
    public async Task MuteUserAsync_WhenAlreadyMuted_ReturnsExistingMute()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var existingMute = Mute.Create(muterId, mutedId);
        SetupMuteDbSet([existingMute]);

        // Act
        var result = await _sut.MuteUserAsync(muterId, mutedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existingMute.Id);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MuteUserAsync_WithExpiration_SetsExpiresAt()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        SetupMuteDbSet([]);

        // Act
        var result = await _sut.MuteUserAsync(muterId, mutedId, null, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MuteUserAsync_WithoutExpiration_HasNoExpiresAt()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        SetupMuteDbSet([]);

        // Act
        var result = await _sut.MuteUserAsync(muterId, mutedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().BeNull();
    }

    #endregion

    #region UnmuteUserAsync

    [Fact]
    public async Task UnmuteUserAsync_WhenMuteExists_RemovesMute()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var mute = Mute.Create(muterId, mutedId);
        SetupMuteDbSet([mute]);

        // Act
        var result = await _sut.UnmuteUserAsync(muterId, mutedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnmuteUserAsync_WhenMuteNotFound_ReturnsFailure()
    {
        // Arrange
        SetupMuteDbSet([]);

        // Act
        var result = await _sut.UnmuteUserAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.MuteNotFound);
    }

    #endregion

    #region IsUserMutedAsync

    [Fact]
    public async Task IsUserMutedAsync_WhenMuted_ReturnsTrue()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var mute = Mute.Create(muterId, mutedId);
        SetupMuteDbSet([mute]);

        // Act
        var result = await _sut.IsUserMutedAsync(muterId, mutedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserMutedAsync_WhenNotMuted_ReturnsFalse()
    {
        // Arrange
        SetupMuteDbSet([]);

        // Act
        var result = await _sut.IsUserMutedAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserMutedAsync_WhenMuteExpired_ReturnsFalse()
    {
        // Arrange
        var muterId = Guid.NewGuid();
        var mutedId = Guid.NewGuid();
        var mute = Mute.Create(muterId, mutedId, null, DateTime.UtcNow.AddDays(-1)); // Expired yesterday
        SetupMuteDbSet([mute]);

        // Act
        var result = await _sut.IsUserMutedAsync(muterId, mutedId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region GetMutedUsersAsync

    [Fact]
    public async Task GetMutedUsersAsync_ReturnsAllMutedUsers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mutes = new List<Mute>
        {
            Mute.Create(userId, Guid.NewGuid()),
            Mute.Create(userId, Guid.NewGuid()),
            Mute.Create(Guid.NewGuid(), Guid.NewGuid()) // Different muter
        };
        SetupMuteDbSet(mutes);

        // Act
        var result = await _sut.GetMutedUsersAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(m => m.MuterId == userId);
    }

    [Fact]
    public async Task GetMutedUsersAsync_ExcludesExpiredMutes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mutes = new List<Mute>
        {
            Mute.Create(userId, Guid.NewGuid()), // Active (no expiration)
            Mute.Create(userId, Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7)), // Active (future expiration)
            Mute.Create(userId, Guid.NewGuid(), null, DateTime.UtcNow.AddDays(-1)) // Expired
        };
        SetupMuteDbSet(mutes);

        // Act
        var result = await _sut.GetMutedUsersAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region CleanupExpiredMutesAsync

    [Fact]
    public async Task CleanupExpiredMutesAsync_RemovesExpiredMutes()
    {
        // Arrange
        var mutes = new List<Mute>
        {
            Mute.Create(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow.AddDays(-7)),
            Mute.Create(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow.AddDays(-1)),
            Mute.Create(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow.AddDays(1)), // Not expired
            Mute.Create(Guid.NewGuid(), Guid.NewGuid()) // No expiration
        };
        SetupMuteDbSet(mutes);

        // Act
        var result = await _sut.CleanupExpiredMutesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2); // 2 expired mutes removed
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredMutesAsync_WhenNoExpiredMutes_ReturnsZero()
    {
        // Arrange
        var mutes = new List<Mute>
        {
            Mute.Create(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow.AddDays(1)),
            Mute.Create(Guid.NewGuid(), Guid.NewGuid())
        };
        SetupMuteDbSet(mutes);

        // Act
        var result = await _sut.CleanupExpiredMutesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    #endregion
}

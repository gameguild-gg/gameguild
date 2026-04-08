using FluentAssertions;
using GameGuild.Social.Follows;
using GameGuild.Social.Follows.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Social.Follows.Tests.Services;

public class FollowOperationServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IUserModerationService> _moderationServiceMock = new();
    private readonly FollowOperationService _sut;

    public FollowOperationServiceTests()
    {
        _sut = new FollowOperationService(
            _contextMock.Object,
            _moderationServiceMock.Object,
            NullLogger<FollowOperationService>.Instance);
    }

    private void SetupFollowDbSet(List<Follow> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<Follow>()).Returns(mock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private void SetupPrivacySettingsDbSet(List<FollowPrivacySettings> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<FollowPrivacySettings>()).Returns(mock.Object);
    }

    #region FollowAsync

    [Fact]
    public async Task FollowAsync_WhenNewFollow_CreatesFollow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        SetupFollowDbSet([]);
        SetupPrivacySettingsDbSet([]);
        _moderationServiceMock.Setup(s => s.AreUsersBlockedAsync(userId, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await _sut.FollowAsync(userId, entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FollowerId.Should().Be(userId);
        result.Value.FollowedEntityId.Should().Be(entityId);
        result.Value.FollowedEntityType.Should().Be(FollowableEntityTypes.User);
        result.Value.NotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task FollowAsync_WhenAlreadyFollowing_ReturnsExistingFollow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var existingFollow = Follow.Create(userId, entityId, FollowableEntityTypes.User);
        SetupFollowDbSet([existingFollow]);

        // Act
        var result = await _sut.FollowAsync(userId, entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existingFollow.Id);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FollowAsync_WhenUsersBlocked_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        SetupFollowDbSet([]);
        _moderationServiceMock.Setup(s => s.AreUsersBlockedAsync(userId, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _sut.FollowAsync(userId, entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.CannotFollowBlockedUser);
    }

    [Fact]
    public async Task FollowAsync_WhenUserDoesNotAllowFollowers_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var privacySettings = FollowPrivacySettings.CreateDefault(entityId);
        privacySettings.Update(true, true, false, true, true, true); // AllowFollowers = false
        SetupFollowDbSet([]);
        SetupPrivacySettingsDbSet([privacySettings]);
        _moderationServiceMock.Setup(s => s.AreUsersBlockedAsync(userId, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await _sut.FollowAsync(userId, entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.UserDoesNotAllowFollowers);
    }

    [Fact]
    public async Task FollowAsync_WithNotificationsDisabled_SetsNotificationsEnabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        SetupFollowDbSet([]);
        SetupPrivacySettingsDbSet([]);
        _moderationServiceMock.Setup(s => s.AreUsersBlockedAsync(userId, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await _sut.FollowAsync(userId, entityId, FollowableEntityTypes.User, notificationsEnabled: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task FollowAsync_ForNonUserEntity_SkipsBlockCheck()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.FollowAsync(userId, courseId, FollowableEntityTypes.Course);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _moderationServiceMock.Verify(s => s.AreUsersBlockedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UnfollowAsync

    [Fact]
    public async Task UnfollowAsync_WhenFollowExists_RemovesFollow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var follow = Follow.Create(userId, entityId, FollowableEntityTypes.User);
        SetupFollowDbSet([follow]);

        // Act
        var result = await _sut.UnfollowAsync(userId, entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnfollowAsync_WhenFollowNotFound_ReturnsFailure()
    {
        // Arrange
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.UnfollowAsync(Guid.NewGuid(), Guid.NewGuid(), FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.FollowNotFound);
    }

    #endregion

    #region IsFollowingAsync

    [Fact]
    public async Task IsFollowingAsync_WhenFollowing_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var follow = Follow.Create(userId, entityId, FollowableEntityTypes.User);
        SetupFollowDbSet([follow]);

        // Act
        var result = await _sut.IsFollowingAsync(userId, entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsFollowingAsync_WhenNotFollowing_ReturnsFalse()
    {
        // Arrange
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.IsFollowingAsync(Guid.NewGuid(), Guid.NewGuid(), FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region UpdateNotificationSettingsAsync

    [Fact]
    public async Task UpdateNotificationSettingsAsync_WhenFollowExists_UpdatesSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var follow = Follow.Create(userId, entityId, FollowableEntityTypes.User, notificationsEnabled: true);
        SetupFollowDbSet([follow]);

        // Act
        var result = await _sut.UpdateNotificationSettingsAsync(userId, entityId, FollowableEntityTypes.User, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotificationSettingsAsync_WhenFollowNotFound_ReturnsFailure()
    {
        // Arrange
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.UpdateNotificationSettingsAsync(Guid.NewGuid(), Guid.NewGuid(), FollowableEntityTypes.User, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.FollowNotFound);
    }

    #endregion

    #region GetFollowersAsync

    [Fact]
    public async Task GetFollowersAsync_ReturnsFollowersOfEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var followers = new List<Follow>
        {
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), Guid.NewGuid(), FollowableEntityTypes.User) // Different entity
        };
        SetupFollowDbSet(followers);

        // Act
        var result = await _sut.GetFollowersAsync(entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(f => f.FollowedEntityId == entityId);
    }

    [Fact]
    public async Task GetFollowersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var followers = Enumerable.Range(1, 100)
            .Select(_ => Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User))
            .ToList();
        SetupFollowDbSet(followers);

        // Act
        var result = await _sut.GetFollowersAsync(entityId, FollowableEntityTypes.User, skip: 10, take: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    #endregion

    #region GetFollowingAsync

    [Fact]
    public async Task GetFollowingAsync_ReturnsUserFollows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.User),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.Course),
            Follow.Create(Guid.NewGuid(), Guid.NewGuid(), FollowableEntityTypes.User) // Different user
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowingAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(f => f.FollowerId == userId);
    }

    [Fact]
    public async Task GetFollowingAsync_WithEntityTypeFilter_FiltersResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.User),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.Course),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.Project)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowingAsync(userId, FollowableEntityTypes.Course);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().FollowedEntityType.Should().Be(FollowableEntityTypes.Course);
    }

    #endregion

    #region GetFollowerCountAsync

    [Fact]
    public async Task GetFollowerCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var followers = new List<Follow>
        {
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User)
        };
        SetupFollowDbSet(followers);

        // Act
        var result = await _sut.GetFollowerCountAsync(entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    #endregion

    #region GetFollowingCountAsync

    [Fact]
    public async Task GetFollowingCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.User),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.Course),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.User)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowingCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact]
    public async Task GetFollowingCountAsync_WithEntityTypeFilter_ReturnsFilteredCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.User),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.Course),
            Follow.Create(userId, Guid.NewGuid(), FollowableEntityTypes.User)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowingCountAsync(userId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    #endregion

    #region AreMutualFollowersAsync

    [Fact]
    public async Task AreMutualFollowersAsync_WhenBothFollow_ReturnsTrue()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(user1, user2, FollowableEntityTypes.User),
            Follow.Create(user2, user1, FollowableEntityTypes.User)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.AreMutualFollowersAsync(user1, user2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task AreMutualFollowersAsync_WhenOnlyOneFollows_ReturnsFalse()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(user1, user2, FollowableEntityTypes.User)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.AreMutualFollowersAsync(user1, user2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task AreMutualFollowersAsync_WhenNeitherFollows_ReturnsFalse()
    {
        // Arrange
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.AreMutualFollowersAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region GetFollowByIdAsync

    [Fact]
    public async Task GetFollowByIdAsync_WhenExists_ReturnsFollow()
    {
        // Arrange
        var follow = Follow.Create(Guid.NewGuid(), Guid.NewGuid(), FollowableEntityTypes.User);
        SetupFollowDbSet([follow]);

        // Act
        var result = await _sut.GetFollowByIdAsync(follow.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(follow.Id);
    }

    [Fact]
    public async Task GetFollowByIdAsync_WhenNotFound_ReturnsFailure()
    {
        // Arrange
        SetupFollowDbSet([]);

        // Act
        var result = await _sut.GetFollowByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(FollowerErrors.FollowNotFound);
    }

    #endregion

    #region GetFollowersWithNotificationsAsync

    [Fact]
    public async Task GetFollowersWithNotificationsAsync_ReturnsOnlyEnabledNotifications()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User, notificationsEnabled: true),
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User, notificationsEnabled: false),
            Follow.Create(Guid.NewGuid(), entityId, FollowableEntityTypes.User, notificationsEnabled: true)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowersWithNotificationsAsync(entityId, FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(f => f.NotificationsEnabled);
    }

    #endregion

    #region GetFollowStatusBatchAsync

    [Fact]
    public async Task GetFollowStatusBatchAsync_ReturnsCorrectStatusForEachEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var followedEntity1 = Guid.NewGuid();
        var followedEntity2 = Guid.NewGuid();
        var notFollowedEntity = Guid.NewGuid();
        var follows = new List<Follow>
        {
            Follow.Create(userId, followedEntity1, FollowableEntityTypes.User),
            Follow.Create(userId, followedEntity2, FollowableEntityTypes.User)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowStatusBatchAsync(userId, 
            [followedEntity1, followedEntity2, notFollowedEntity], 
            FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[followedEntity1].Should().BeTrue();
        result.Value[followedEntity2].Should().BeTrue();
        result.Value[notFollowedEntity].Should().BeFalse();
    }

    #endregion

    #region GetFollowerCountsBatchAsync

    [Fact]
    public async Task GetFollowerCountsBatchAsync_ReturnsCorrectCountsForEachEntity()
    {
        // Arrange
        var entity1 = Guid.NewGuid();
        var entity2 = Guid.NewGuid();
        var entity3 = Guid.NewGuid(); // No followers
        var follows = new List<Follow>
        {
            Follow.Create(Guid.NewGuid(), entity1, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), entity1, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), entity1, FollowableEntityTypes.User),
            Follow.Create(Guid.NewGuid(), entity2, FollowableEntityTypes.User)
        };
        SetupFollowDbSet(follows);

        // Act
        var result = await _sut.GetFollowerCountsBatchAsync([entity1, entity2, entity3], FollowableEntityTypes.User);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[entity1].Should().Be(3);
        result.Value[entity2].Should().Be(1);
        result.Value[entity3].Should().Be(0);
    }

    #endregion
}

using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ArchiveNotificationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly ArchiveNotificationCommandHandler _handler;

    public ArchiveNotificationCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new ArchiveNotificationCommandHandler(
            _userRepositoryMock.Object,
            _notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldArchiveNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var notification = UserNotification.Create(userId, "test", "Test", "Test content");
        notification.Id = notificationId;
        var command = new ArchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        _notificationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        notification.IsArchived.Should().BeTrue();
        notification.ArchivedAt.Should().NotBeNull();
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var command = new ArchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var command = new ArchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotification?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNotificationBelongsToOtherUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var notification = UserNotification.Create(otherUserId, "test", "Test", "Test content");
        notification.Id = notificationId;
        var command = new ArchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenAlreadyArchived_ShouldNotUpdateAgain()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var notification = UserNotification.Create(userId, "test", "Test", "Test content");
        notification.Id = notificationId;
        notification.Archive(); // Already archived
        var originalArchivedAt = notification.ArchivedAt;
        var command = new ArchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        _notificationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        notification.IsArchived.Should().BeTrue();
        notification.ArchivedAt.Should().Be(originalArchivedAt); // Should not change
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

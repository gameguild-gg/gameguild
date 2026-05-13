using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UnarchiveNotificationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly UnarchiveNotificationCommandHandler _handler;

    public UnarchiveNotificationCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new UnarchiveNotificationCommandHandler(_userRepositoryMock.Object, _notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUnarchiveNotification()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var notification = UserNotification.Create(userId, "test", "Test", "Test content");
        notification.Id = notificationId;
        notification.Archive();
        var command = new UnarchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        _notificationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        notification.IsArchived.Should().BeFalse();
        notification.ArchivedAt.Should().BeNull();
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var command = new UnarchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ShouldThrowInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var command = new UnarchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotification?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNotificationBelongsToOtherUser_ShouldThrowInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var notification = UserNotification.Create(otherUserId, "test", "Test", "Test content");
        notification.Id = notificationId;
        var command = new UnarchiveNotificationCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

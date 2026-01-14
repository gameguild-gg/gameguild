using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class MarkNotificationAsUnreadCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly MarkNotificationAsUnreadCommandHandler _handler;

    public MarkNotificationAsUnreadCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new MarkNotificationAsUnreadCommandHandler(
            _userRepositoryMock.Object,
            _notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldMarkNotificationAsUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var notification = UserNotification.Create(userId, "test", "Test", "Test content");
        notification.Id = notificationId;
        notification.MarkAsRead(); // Mark as read first
        var command = new MarkNotificationAsUnreadCommand(userId, notificationId);

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
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var command = new MarkNotificationAsUnreadCommand(userId, notificationId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

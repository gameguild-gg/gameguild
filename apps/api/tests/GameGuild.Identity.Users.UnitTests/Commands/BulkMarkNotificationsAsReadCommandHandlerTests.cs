using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkMarkNotificationsAsReadCommandHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly BulkMarkNotificationsAsReadCommandHandler _handler;

    public BulkMarkNotificationsAsReadCommandHandlerTests()
    {
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new BulkMarkNotificationsAsReadCommandHandler(_notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNotificationIds_ShouldMarkAllNotificationsAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var notifications = notificationIds.Select(id =>
        {
            var notification = UserNotification.Create(userId, "test", "Title", "Content");
            notification.Id = id;
            return notification;
        }).ToList();
        var command = new BulkMarkNotificationsAsReadCommand(userId, notificationIds);

        _notificationRepositoryMock
            .Setup(x => x.GetByIdsAsync(userId, notificationIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);
        _notificationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        notifications.Should().OnlyContain(notification => notification.IsRead);
        notifications.Should().OnlyContain(notification => notification.ReadAt.HasValue);
        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()),
            Times.Exactly(notificationIds.Count));
        _notificationRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyNotificationIds_ShouldNotTouchRepository()
    {
        // Arrange
        var command = new BulkMarkNotificationsAsReadCommand(Guid.NewGuid(), new List<Guid>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _notificationRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

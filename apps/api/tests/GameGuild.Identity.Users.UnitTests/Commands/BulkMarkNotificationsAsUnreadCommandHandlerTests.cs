using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkMarkNotificationsAsUnreadCommandHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly BulkMarkNotificationsAsUnreadCommandHandler _handler;

    public BulkMarkNotificationsAsUnreadCommandHandlerTests()
    {
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new BulkMarkNotificationsAsUnreadCommandHandler(_notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNotificationIds_ShouldMarkAllNotificationsAsUnread()
    {
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var notifications = notificationIds.Select(id =>
        {
            var notification = UserNotification.Create(userId, "test", "Title", "Content");
            notification.Id = id;
            notification.MarkAsRead();
            return notification;
        }).ToList();
        var command = new BulkMarkNotificationsAsUnreadCommand(userId, notificationIds);

        _notificationRepositoryMock
            .Setup(x => x.GetByIdsAsync(userId, notificationIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);
        _notificationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        notifications.Should().OnlyContain(notification => !notification.IsRead);
        notifications.Should().OnlyContain(notification => notification.ReadAt == null);
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
        var command = new BulkMarkNotificationsAsUnreadCommand(Guid.NewGuid(), new List<Guid>());

        var result = await _handler.Handle(command, CancellationToken.None);

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

using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkUnarchiveNotificationsCommandHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly BulkUnarchiveNotificationsCommandHandler _handler;

    public BulkUnarchiveNotificationsCommandHandlerTests()
    {
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new BulkUnarchiveNotificationsCommandHandler(_notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNotificationIds_ShouldUnarchiveAllNotifications()
    {
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var notifications = notificationIds.Select(id =>
        {
            var notification = UserNotification.Create(userId, "test", "Title", "Content");
            notification.Id = id;
            notification.Archive();
            return notification;
        }).ToList();
        var command = new BulkUnarchiveNotificationsCommand(userId, notificationIds);

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
        notifications.Should().OnlyContain(notification => !notification.IsArchived);
        notifications.Should().OnlyContain(notification => notification.ArchivedAt == null);
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
        var command = new BulkUnarchiveNotificationsCommand(Guid.NewGuid(), new List<Guid>());

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

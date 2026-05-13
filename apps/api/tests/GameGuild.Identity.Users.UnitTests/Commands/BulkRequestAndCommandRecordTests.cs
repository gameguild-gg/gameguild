using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkRequestAndCommandRecordTests
{
    [Fact]
    public void BulkActivateUsersRequest_ShouldExposeUserIds()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var request = new BulkActivateUsersRequest(userIds);

        request.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void BulkCreateUsersRequest_ShouldExposeUsers()
    {
        var users = new[]
        {
            new CreateUserRequestItem("one@example.com", "One"),
            new CreateUserRequestItem("two@example.com", "Two", "+15550001")
        };

        var request = new BulkCreateUsersRequest(users);

        request.Users.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void BulkDeactivateUsersRequest_ShouldExposeUserIds()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var request = new BulkDeactivateUsersRequest(userIds);

        request.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void BulkDeleteUsersRequest_ShouldExposeUserIds()
    {
        var userIds = new[] { Guid.NewGuid() };

        var request = new BulkDeleteUsersRequest(userIds);

        request.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void BulkSuspendUsersRequest_ShouldExposeUserIds()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var request = new BulkSuspendUsersRequest(userIds);

        request.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void BulkUnsuspendUsersRequest_ShouldExposeUserIds()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var request = new BulkUnsuspendUsersRequest(userIds);

        request.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void BulkUpdateUsersRequest_ShouldExposeUpdates()
    {
        var updates = new[]
        {
            new UpdateUserRequestItem(Guid.NewGuid(), "Updated One"),
            new UpdateUserRequestItem(Guid.NewGuid(), "Updated Two", "+15550002")
        };

        var request = new BulkUpdateUsersRequest(updates);

        request.Updates.Should().BeEquivalentTo(updates);
    }

    [Fact]
    public void BulkRestoreUsersRequest_ShouldExposeUserIds()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var request = new BulkRestoreUsersRequest(userIds);

        request.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void BulkPurgeUsersRequest_ShouldExposeProperties()
    {
        var userIds = new[] { Guid.NewGuid() };

        var request = new BulkPurgeUsersRequest(userIds, PurgeStrategy.Scheduled);

        request.UserIds.Should().BeEquivalentTo(userIds);
        request.Strategy.Should().Be(PurgeStrategy.Scheduled);
    }

    [Fact]
    public void BulkArchiveNotificationsCommand_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var command = new BulkArchiveNotificationsCommand(userId, notificationIds);

        command.UserId.Should().Be(userId);
        command.NotificationIds.Should().BeEquivalentTo(notificationIds);
    }

    [Fact]
    public void BulkMarkNotificationsAsReadCommand_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid() };

        var command = new BulkMarkNotificationsAsReadCommand(userId, notificationIds);

        command.UserId.Should().Be(userId);
        command.NotificationIds.Should().BeEquivalentTo(notificationIds);
    }

    [Fact]
    public void BulkMarkNotificationsAsUnreadCommand_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var command = new BulkMarkNotificationsAsUnreadCommand(userId, notificationIds);

        command.UserId.Should().Be(userId);
        command.NotificationIds.Should().BeEquivalentTo(notificationIds);
    }

    [Fact]
    public void BulkUnarchiveNotificationsCommand_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid() };

        var command = new BulkUnarchiveNotificationsCommand(userId, notificationIds);

        command.UserId.Should().Be(userId);
        command.NotificationIds.Should().BeEquivalentTo(notificationIds);
    }

    [Fact]
    public void UnarchiveNotificationCommand_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        var command = new UnarchiveNotificationCommand(userId, notificationId);

        command.UserId.Should().Be(userId);
        command.NotificationId.Should().Be(notificationId);
    }
}

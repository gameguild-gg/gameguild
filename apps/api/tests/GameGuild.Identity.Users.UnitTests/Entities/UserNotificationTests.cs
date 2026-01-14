using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public class UserNotificationTests
{
    [Fact]
    public void Create_ShouldInitializeWithRequiredFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "message";
        var title = "New Message";
        var content = "You have a new message";

        // Act
        var notification = UserNotification.Create(userId, type, title, content);

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Title.Should().Be(title);
        notification.Content.Should().Be(content);
        notification.IsRead.Should().BeFalse();
        notification.IsArchived.Should().BeFalse();
        notification.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void MarkAsRead_ShouldSetIsReadToTrue()
    {
        // Arrange
        var notification = UserNotification.Create(Guid.NewGuid(), "test", "Title", "Message");

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsUnread_ShouldSetIsReadToFalse()
    {
        // Arrange
        var notification = UserNotification.Create(Guid.NewGuid(), "test", "Title", "Message");
        notification.MarkAsRead();

        // Act
        notification.MarkAsUnread();

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Archive_ShouldSetIsArchivedToTrue()
    {
        // Arrange
        var notification = UserNotification.Create(Guid.NewGuid(), "test", "Title", "Message");

        // Act
        notification.Archive();

        // Assert
        notification.IsArchived.Should().BeTrue();
        notification.ArchivedAt.Should().NotBeNull();
        notification.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Unarchive_ShouldSetIsArchivedToFalse()
    {
        // Arrange
        var notification = UserNotification.Create(Guid.NewGuid(), "test", "Title", "Message");
        notification.Archive();

        // Act
        notification.Unarchive();

        // Assert
        notification.IsArchived.Should().BeFalse();
        notification.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void SetPriority_ShouldUpdatePriorityField()
    {
        // Arrange
        var notification = UserNotification.Create(Guid.NewGuid(), "test", "Title", "Message");

        // Act
        notification.Priority = NotificationPriority.High;

        // Assert
        notification.Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    public void SetActionUrl_ShouldUpdateActionUrlField()
    {
        // Arrange
        var notification = UserNotification.Create(Guid.NewGuid(), "test", "Title", "Message");
        var actionUrl = "/messages/123";

        // Act
        notification.ActionUrl = actionUrl;

        // Assert
        notification.ActionUrl.Should().Be(actionUrl);
    }
}

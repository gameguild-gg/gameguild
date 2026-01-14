using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public class UserNotificationMethodsTests
{
    [Fact]
    public void MarkAsRead_WhenUnread_ShouldSetReadProperties()
    {
        // Arrange
        var notification = new UserNotification { IsRead = false };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        notification.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldNotChangeProperties()
    {
        // Arrange
        var readAt = DateTime.UtcNow.AddMinutes(-5);
        var notification = new UserNotification { IsRead = true, ReadAt = readAt };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().Be(readAt);
        notification.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void MarkAsUnread_WhenRead_ShouldClearReadProperties()
    {
        // Arrange
        var notification = new UserNotification { IsRead = true, ReadAt = DateTime.UtcNow.AddMinutes(-5) };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.MarkAsUnread();

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void MarkAsUnread_WhenAlreadyUnread_ShouldNotChangeProperties()
    {
        // Arrange
        var notification = new UserNotification { IsRead = false, ReadAt = null };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.MarkAsUnread();

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Archive_WhenNotArchived_ShouldSetArchivedProperties()
    {
        // Arrange
        var notification = new UserNotification { IsArchived = false };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.Archive();

        // Assert
        notification.IsArchived.Should().BeTrue();
        notification.ArchivedAt.Should().NotBeNull();
        notification.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        notification.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldNotChangeProperties()
    {
        // Arrange
        var archivedAt = DateTime.UtcNow.AddHours(-1);
        var notification = new UserNotification { IsArchived = true, ArchivedAt = archivedAt };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.Archive();

        // Assert
        notification.IsArchived.Should().BeTrue();
        notification.ArchivedAt.Should().Be(archivedAt);
        notification.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Unarchive_WhenArchived_ShouldClearArchivedProperties()
    {
        // Arrange
        var notification = new UserNotification { IsArchived = true, ArchivedAt = DateTime.UtcNow.AddHours(-1) };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.Unarchive();

        // Assert
        notification.IsArchived.Should().BeFalse();
        notification.ArchivedAt.Should().BeNull();
        notification.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Unarchive_WhenNotArchived_ShouldNotChangeProperties()
    {
        // Arrange
        var notification = new UserNotification { IsArchived = false, ArchivedAt = null };
        var originalUpdatedAt = notification.UpdatedAt;

        // Act
        notification.Unarchive();

        // Assert
        notification.IsArchived.Should().BeFalse();
        notification.ArchivedAt.Should().BeNull();
        notification.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Create_WithRequiredParams_ShouldCreateValidNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "System";
        var title = "Welcome";
        var content = "Welcome to the platform!";

        // Act
        var notification = UserNotification.Create(userId, type, title, content);

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Title.Should().Be(title);
        notification.Content.Should().Be(content);
        notification.Priority.Should().Be(NotificationPriority.Normal);
        notification.IsRead.Should().BeFalse();
        notification.IsArchived.Should().BeFalse();
        notification.SenderId.Should().BeNull();
        notification.Source.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllParams_ShouldSetAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var type = "Alert";
        var title = "Security Alert";
        var content = "Suspicious login detected";
        var priority = NotificationPriority.High;
        var source = "SecuritySystem";

        // Act
        var notification = UserNotification.Create(userId, type, title, content, priority, senderId, source);

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Title.Should().Be(title);
        notification.Content.Should().Be(content);
        notification.Priority.Should().Be(priority);
        notification.SenderId.Should().Be(senderId);
        notification.Source.Should().Be(source);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidType_ShouldThrowArgumentException(string? invalidType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Title";
        var content = "Content";

        // Act & Assert
        var act = () => UserNotification.Create(userId, invalidType!, title, content);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "System";
        var content = "Content";

        // Act & Assert
        var act = () => UserNotification.Create(userId, type, invalidTitle!, content);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidContent_ShouldThrowArgumentException(string? invalidContent)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "System";
        var title = "Title";

        // Act & Assert
        var act = () => UserNotification.Create(userId, type, title, invalidContent!);
        act.Should().Throw<ArgumentException>();
    }
}
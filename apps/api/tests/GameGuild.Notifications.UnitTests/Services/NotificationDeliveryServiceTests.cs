using FluentAssertions;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services;

public class NotificationDeliveryServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<INotificationPreferenceService> _preferenceServiceMock = new();
    private readonly Mock<INotificationTemplateService> _templateServiceMock = new();
    private readonly NotificationDeliveryService _sut;

    public NotificationDeliveryServiceTests()
    {
        _sut = new NotificationDeliveryService(
            _contextMock.Object,
            _preferenceServiceMock.Object,
            _templateServiceMock.Object,
            NullLogger<NotificationDeliveryService>.Instance);
    }

    private void SetupNotificationDbSet(List<Notification> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<Notification>()).Returns(mock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private void SetupTemplateDbSet(List<NotificationTemplate> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<NotificationTemplate>()).Returns(mock.Object);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenNotificationExists_ReturnsSuccess()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "Title", "Message");
        SetupNotificationDbSet([notification]);

        // Act
        var result = await _sut.GetByIdAsync(notification.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotificationNotFound_ReturnsFailure()
    {
        // Arrange
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    #endregion

    #region GetUserNotificationsAsync

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsUserNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Title 1", "Msg"),
            Notification.Create(userId, NotificationType.Security, NotificationChannel.InApp, "Title 2", "Msg"),
            Notification.Create(Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "Other User", "Msg")
        };
        SetupNotificationDbSet(notifications);

        // Act
        var result = await _sut.GetUserNotificationsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(n => n.RecipientId == userId);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithIsReadFilter_ReturnsFilteredNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var readNotification = Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Read", "Msg");
        readNotification.MarkAsRead();
        var unreadNotification = Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Unread", "Msg");
        SetupNotificationDbSet([readNotification, unreadNotification]);

        // Act
        var result = await _sut.GetUserNotificationsAsync(userId, isRead: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Read");
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = Enumerable.Range(1, 30)
            .Select(i => Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, $"Title {i}", "Msg"))
            .ToList();
        SetupNotificationDbSet(notifications);

        // Act
        var result = await _sut.GetUserNotificationsAsync(userId, skip: 10, take: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    #endregion

    #region GetUnreadCountAsync

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var readNotification = Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Read", "Msg");
        readNotification.MarkAsRead();
        var unreadNotifications = Enumerable.Range(1, 5)
            .Select(_ => Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Unread", "Msg"))
            .ToList();
        var allNotifications = new List<Notification> { readNotification };
        allNotifications.AddRange(unreadNotifications);
        SetupNotificationDbSet(allNotifications);

        // Act
        var result = await _sut.GetUnreadCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    #endregion

    #region SendAsync

    [Fact]
    public async Task SendAsync_WhenPreferencesAllowSending_CreatesNotification()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            recipientId, NotificationType.System, NotificationChannel.InApp, 
            NotificationPriority.Normal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendAsync(recipientId, NotificationType.System, "Title", "Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecipientId.Should().Be(recipientId);
        result.Value.Title.Should().Be("Title");
        result.Value.IsSent.Should().BeTrue(); // InApp notifications are marked as sent immediately
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_WhenPreferencesBlockSending_ReturnsSkipped()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            recipientId, NotificationType.Marketing, NotificationChannel.Email, 
            NotificationPriority.Normal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendAsync(recipientId, NotificationType.Marketing, "Title", "Message", NotificationChannel.Email);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Notification.Skipped");
    }

    [Fact]
    public async Task SendAsync_WithOptionalParams_SetsAllProperties()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var refEntityId = Guid.NewGuid();
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendAsync(
            recipientId, NotificationType.Security, "Security Alert", "Your account was accessed",
            NotificationChannel.Email, tenantId, "/security", NotificationPriority.High,
            refEntityId, "User", "{\"ip\":\"1.2.3.4\"}");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(NotificationType.Security);
        result.Value.Channel.Should().Be(NotificationChannel.Email);
        result.Value.ActionUrl.Should().Be("/security");
        result.Value.Priority.Should().Be(NotificationPriority.High);
        result.Value.ReferenceEntityId.Should().Be(refEntityId);
        result.Value.ReferenceEntityType.Should().Be("User");
        result.Value.Metadata.Should().Contain("ip");
    }

    [Fact]
    public async Task SendAsync_EmailChannel_DoesNotMarkAsSentImmediately()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendAsync(recipientId, NotificationType.System, "Title", "Message", NotificationChannel.Email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSent.Should().BeFalse(); // Email notifications need external delivery
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SendFromTemplateAsync

    [Fact]
    public async Task SendFromTemplateAsync_WhenTemplateExists_SendsFromTemplate()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var template = NotificationTemplate.Create(
            "welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.InApp,
            "Welcome {{name}}!", "Hello {{name}}, welcome to the platform!");
        SetupNotificationDbSet([]);
        SetupTemplateDbSet([template]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _templateServiceMock.Setup(s => s.ReplacePlaceholders(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns<string, Dictionary<string, string>>((t, p) => t.Replace("{{name}}", p["name"]));

        // Act
        var result = await _sut.SendFromTemplateAsync(
            recipientId, "welcome", new Dictionary<string, string> { { "name", "Alice" } });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Welcome Alice!");
        result.Value.Message.Should().Be("Hello Alice, welcome to the platform!");
    }

    [Fact]
    public async Task SendFromTemplateAsync_WhenTemplateNotFound_ReturnsFailure()
    {
        // Arrange
        SetupNotificationDbSet([]);
        SetupTemplateDbSet([]);

        // Act
        var result = await _sut.SendFromTemplateAsync(
            Guid.NewGuid(), "nonexistent", new Dictionary<string, string>());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }

    [Fact]
    public async Task SendFromTemplateAsync_WhenTemplateInactive_ReturnsFailure()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "inactive_template", "Inactive", NotificationType.System, NotificationChannel.InApp, "T", "M");
        template.Deactivate();
        SetupNotificationDbSet([]);
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.SendFromTemplateAsync(
            Guid.NewGuid(), "inactive_template", new Dictionary<string, string>());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }

    #endregion

    #region SendBulkAsync

    [Fact]
    public async Task SendBulkAsync_SendsToAllAllowedRecipients()
    {
        // Arrange
        var recipients = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendBulkAsync(
            recipients, NotificationType.System, "Bulk Title", "Bulk Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task SendBulkAsync_SkipsRecipientsWithBlockingPreferences()
    {
        // Arrange
        var allowedRecipient = Guid.NewGuid();
        var blockedRecipient = Guid.NewGuid();
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            allowedRecipient, It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            blockedRecipient, It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendBulkAsync(
            [allowedRecipient, blockedRecipient], NotificationType.Marketing, "Title", "Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().RecipientId.Should().Be(allowedRecipient);
    }

    [Fact]
    public async Task SendBulkAsync_WhenAllBlocked_ReturnsEmptyList()
    {
        // Arrange
        SetupNotificationDbSet([]);
        _preferenceServiceMock.Setup(s => s.ShouldSendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(), 
            It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendBulkAsync(
            [Guid.NewGuid()], NotificationType.Marketing, "Title", "Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region ScheduleAsync

    [Fact]
    public async Task ScheduleAsync_WithFutureTime_CreatesScheduledNotification()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(1);
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.ScheduleAsync(
            recipientId, NotificationType.System, "Scheduled", "Message", scheduledAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ScheduledAt.Should().Be(scheduledAt);
        result.Value.IsSent.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleAsync_WithPastTime_ReturnsValidationError()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(-1);
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.ScheduleAsync(
            recipientId, NotificationType.System, "Scheduled", "Message", scheduledAt);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Notification.InvalidSchedule");
    }

    #endregion

    #region MarkAsReadAsync

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExists_MarksAsRead()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "Title", "Message");
        SetupNotificationDbSet([notification]);

        // Act
        var result = await _sut.MarkAsReadAsync(notification.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationNotFound_ReturnsFailure()
    {
        // Arrange
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.MarkAsReadAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    #endregion

    #region MarkAllAsReadAsync

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnreadNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Title 1", "Msg"),
            Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Title 2", "Msg"),
            Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Title 3", "Msg")
        };
        SetupNotificationDbSet(notifications);

        // Act
        var result = await _sut.MarkAllAsReadAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notifications.Should().OnlyContain(n => n.IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenNoUnreadNotifications_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.MarkAllAsReadAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region MarkAsUnreadAsync

    [Fact]
    public async Task MarkAsUnreadAsync_WhenNotificationExists_MarksAsUnread()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "Title", "Message");
        notification.MarkAsRead();
        SetupNotificationDbSet([notification]);

        // Act
        var result = await _sut.MarkAsUnreadAsync(notification.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsUnreadAsync_WhenNotificationNotFound_ReturnsFailure()
    {
        // Arrange
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.MarkAsUnreadAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenNotificationExists_SoftDeletes()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "Title", "Message");
        SimulatePersisted(notification);
        SetupNotificationDbSet([notification]);

        // Act
        var result = await _sut.DeleteAsync(notification.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationNotFound_ReturnsFailure()
    {
        // Arrange
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    #endregion

    #region DeleteReadNotificationsAsync

    [Fact]
    public async Task DeleteReadNotificationsAsync_DeletesOnlyReadNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var readNotifications = new List<Notification>
        {
            Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Read 1", "Msg"),
            Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Read 2", "Msg")
        };
        foreach (var n in readNotifications)
        {
            n.MarkAsRead();
            SimulatePersisted(n);
        }
        var unreadNotification = Notification.Create(userId, NotificationType.System, NotificationChannel.InApp, "Unread", "Msg");
        SimulatePersisted(unreadNotification);
        
        var allNotifications = new List<Notification>(readNotifications) { unreadNotification };
        SetupNotificationDbSet(allNotifications);

        // Act
        var result = await _sut.DeleteReadNotificationsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        readNotifications.Should().OnlyContain(n => n.IsDeleted);
        unreadNotification.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteReadNotificationsAsync_WhenNoReadNotifications_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupNotificationDbSet([]);

        // Act
        var result = await _sut.DeleteReadNotificationsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    #endregion

    #region Helpers

    private static void SimulatePersisted<T>(T entity) where T : EntityBase
    {
        var versionProperty = typeof(EntityBase).GetProperty("Version", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        versionProperty?.SetValue(entity, 1);
    }

    #endregion
}

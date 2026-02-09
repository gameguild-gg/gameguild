using FluentAssertions;
using Xunit;

namespace GameGuild.Notifications.Tests;

/// <summary>
/// Unit tests for NotificationTemplateService.ReplacePlaceholders — pure function.
/// </summary>
public class NotificationTemplateServiceTests
{
    [Fact]
    public void ReplacePlaceholders_ShouldReplaceAllOccurrences()
    {
        // Arrange
        var service = CreateServiceForPlaceholderTests();
        var template = "Hello {{name}}, your course {{course}} starts on {{date}}.";
        var placeholders = new Dictionary<string, string>
        {
            { "name", "Alice" },
            { "course", "Game Design 101" },
            { "date", "2025-01-15" }
        };

        // Act
        var result = service.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().Be("Hello Alice, your course Game Design 101 starts on 2025-01-15.");
    }

    [Fact]
    public void ReplacePlaceholders_WhenNoPlaceholders_ShouldReturnOriginalTemplate()
    {
        var service = CreateServiceForPlaceholderTests();
        var template = "No placeholders here.";

        var result = service.ReplacePlaceholders(template, new Dictionary<string, string>());

        result.Should().Be("No placeholders here.");
    }

    [Fact]
    public void ReplacePlaceholders_WhenPlaceholderNotInTemplate_ShouldLeaveTemplateUnchanged()
    {
        var service = CreateServiceForPlaceholderTests();
        var template = "Hello {{name}}!";
        var placeholders = new Dictionary<string, string> { { "missing", "value" } };

        var result = service.ReplacePlaceholders(template, placeholders);

        result.Should().Be("Hello {{name}}!"); // Unreplaced placeholder stays
    }

    private static Services.NotificationTemplateService CreateServiceForPlaceholderTests()
    {
        return new Services.NotificationTemplateService(
            null!, // Context not needed for ReplacePlaceholders
            null!  // Logger not needed for ReplacePlaceholders
        );
    }
}

/// <summary>
/// Unit tests for Notification entity.
/// </summary>
public class NotificationEntityTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var recipientId = Guid.NewGuid();
        var notification = Notification.Create(
            recipientId,
            NotificationType.CourseEnrollment,
            NotificationChannel.InApp,
            "Enrolled!",
            "You have been enrolled in a course.",
            tenantId: Guid.NewGuid(),
            actionUrl: "/courses/123",
            priority: NotificationPriority.High);

        notification.RecipientId.Should().Be(recipientId);
        notification.Type.Should().Be(NotificationType.CourseEnrollment);
        notification.Channel.Should().Be(NotificationChannel.InApp);
        notification.Title.Should().Be("Enrolled!");
        notification.Message.Should().Be("You have been enrolled in a course.");
        notification.Priority.Should().Be(NotificationPriority.High);
        notification.IsRead.Should().BeFalse();
        notification.IsSent.Should().BeFalse();
    }

    [Fact]
    public void MarkAsRead_ShouldBeIdempotent()
    {
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "T", "M");

        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        notification.MarkAsRead(); // Second call should not change ReadAt

        notification.ReadAt.Should().Be(firstReadAt);
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsSent_ShouldBeIdempotent()
    {
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "T", "M");

        notification.MarkAsSent();
        var firstSentAt = notification.SentAt;

        notification.MarkAsSent(); // Second call

        notification.SentAt.Should().Be(firstSentAt);
        notification.IsSent.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUnread_ShouldClearReadState()
    {
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "T", "M");
        notification.MarkAsRead();

        notification.MarkAsUnread();

        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }
}

/// <summary>
/// Unit tests for NotificationPreference entity.
/// </summary>
public class NotificationPreferenceEntityTests
{
    [Fact]
    public void CreateDefault_ShouldEnableAllChannelsExceptSms()
    {
        var prefs = NotificationPreference.CreateDefault(Guid.NewGuid());

        prefs.EmailEnabled.Should().BeTrue();
        prefs.PushEnabled.Should().BeTrue();
        prefs.InAppEnabled.Should().BeTrue();
        prefs.SmsEnabled.Should().BeFalse();
        prefs.MarketingEnabled.Should().BeTrue();
        prefs.SocialEnabled.Should().BeTrue();
        prefs.LearningEnabled.Should().BeTrue();
        prefs.AchievementsEnabled.Should().BeTrue();
        prefs.QuietHoursBypassPriority.Should().Be(NotificationPriority.Urgent);
    }
}

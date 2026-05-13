using FluentAssertions;

using static GameGuild.Identity.Users.UnitTests.JsonTestData;
using Xunit;

namespace GameGuild.Identity.Users.Tests;

/// <summary>
///     Coverage boost tests for all notification-related DTOs and request models.
/// </summary>
public class UserNotificationModelTests
{
    [Fact]
    public void UserNotificationDto_ShouldInstantiate()
    {
        var dto = new UserNotificationDto(
            Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Type: "System",
            Title: "Test",
            Message: "Test message",
            Priority: "Normal",
            Category: "General",
            IsRead: false,
            IsArchived: false,
            ReadAt: null,
            ArchivedAt: null,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(7),
            ActionUrl: "https://example.com",
            ActionText: "View",
            ImageUrl: "https://img.example.com/1.png",
            Metadata: JsonMap(new Dictionary<string, object?> { ["key"] = "value" }),
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null,
            Version: new byte[] { 1, 2, 3 }
        );

        dto.Title.Should().Be("Test");
        dto.Priority.Should().Be("Normal");
        dto.IsRead.Should().BeFalse();
        dto.Metadata.Should().ContainKey("key");
    }

    [Fact]
    public void UserNotificationCountDto_ShouldInstantiate()
    {
        var dto = new UserNotificationCountDto(
            Total: 10, Unread: 5, Archived: 2,
            ByPriority: new Dictionary<string, int> { ["High"] = 3, ["Normal"] = 7 },
            ByCategory: new Dictionary<string, int> { ["System"] = 4, ["Social"] = 6 });

        dto.Total.Should().Be(10);
        dto.Unread.Should().Be(5);
    }

    [Fact]
    public void UserNotificationDetailDto_ShouldInstantiate()
    {
        var notif = new UserNotificationDto(Guid.NewGuid(), Guid.NewGuid(), "System", "T", "M",
            "Normal", null, false, false, null, null, null, null, null, null,
            JsonMap(new Dictionary<string, object?>()), DateTimeOffset.UtcNow, null, Array.Empty<byte>());

        var action = new NotificationActionDto("a1", "Click", "https://x.com", "link", true);

        var dto = new UserNotificationDetailDto(notif, new List<UserNotificationDto>(), new List<NotificationActionDto> { action });

        dto.Notification.Should().Be(notif);
        dto.Actions.Should().HaveCount(1);
    }

    [Fact]
    public void NotificationActionDto_ShouldInstantiate()
    {
        var dto = new NotificationActionDto("a1", "View Details", "https://x.com/details", "navigate", true);

        dto.Id.Should().Be("a1");
        dto.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void ExecuteNotificationActionRequest_ShouldInstantiate()
    {
        var req = new ExecuteNotificationActionRequest("action1", JsonMap(new Dictionary<string, object?> { ["param"] = 42 }));

        req.ActionId.Should().Be("action1");
        req.Parameters.Should().ContainKey("param");
    }

    [Fact]
    public void NotificationActionResultDto_ShouldInstantiate()
    {
        var result = new NotificationActionResultDto(true, "OK", "https://redirect.com", null);

        result.Success.Should().BeTrue();
        result.RedirectUrl.Should().Be("https://redirect.com");
    }

    [Fact]
    public void UserNotificationDeliverySettingsDto_ShouldInstantiate()
    {
        var dto = new UserNotificationDeliverySettingsDto(
            UserId: Guid.NewGuid(),
            EmailEnabled: true,
            PushEnabled: false,
            SmsEnabled: false,
            InAppEnabled: true,
            EmailFrequency: "Daily",
            PushFrequency: "Realtime",
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(8, 0),
            TimeZone: "America/New_York",
            CategorySettings: new Dictionary<string, NotificationCategorySettingsDto>
            {
                ["System"] = new NotificationCategorySettingsDto(true, true, false, false, "Normal")
            });

        dto.EmailEnabled.Should().BeTrue();
        dto.QuietHoursStart.Should().NotBeNull();
        dto.CategorySettings.Should().ContainKey("System");
    }

    [Fact]
    public void NotificationCategorySettingsDto_ShouldInstantiate()
    {
        var dto = new NotificationCategorySettingsDto(true, true, false, false, "High");

        dto.Enabled.Should().BeTrue();
        dto.Priority.Should().Be("High");
    }

    [Fact]
    public void UpdateUserNotificationDeliverySettingsRequest_ShouldInstantiate()
    {
        var req = new UpdateUserNotificationDeliverySettingsRequest(
            EmailEnabled: true,
            PushEnabled: false,
            SmsEnabled: true,
            InAppEnabled: true,
            EmailFrequency: "Weekly",
            PushFrequency: "Realtime",
            QuietHoursStart: new TimeOnly(23, 0),
            QuietHoursEnd: new TimeOnly(7, 0),
            TimeZone: "UTC",
            CategorySettings: null);

        req.EmailEnabled.Should().BeTrue();
        req.TimeZone.Should().Be("UTC");
    }

    [Fact]
    public void BulkNotificationRequest_ShouldInstantiate()
    {
        var req = new BulkNotificationRequest(
            new List<Guid> { Guid.NewGuid() }, "markRead",
            new NotificationFilterCriteria(
                Categories: new List<string> { "System" },
                Priorities: new List<string> { "High" },
                Types: new List<string> { "Alert" },
                IsRead: false,
                IsArchived: null,
                DateFrom: DateTimeOffset.UtcNow.AddDays(-7),
                DateTo: DateTimeOffset.UtcNow));

        req.NotificationIds.Should().HaveCount(1);
        req.FilterCriteria!.Categories.Should().Contain("System");
    }

    [Fact]
    public void NotificationFilterCriteria_DefaultValues_ShouldBeNull()
    {
        var criteria = new NotificationFilterCriteria();

        criteria.Categories.Should().BeNull();
        criteria.IsRead.Should().BeNull();
    }
}

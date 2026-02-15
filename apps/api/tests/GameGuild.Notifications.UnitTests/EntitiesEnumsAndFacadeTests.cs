using FluentAssertions;
using GameGuild;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Moq;
using Xunit;

namespace GameGuild.Notifications.Tests;

/// <summary>
/// Tests for NotificationTemplate entity methods.
/// </summary>
public class NotificationTemplateEntityTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var template = NotificationTemplate.Create(
            "welcome_email", "Welcome", NotificationType.Onboarding,
            NotificationChannel.Email, "Welcome {{name}}", "Hello {{name}}!",
            description: "Welcome template", actionUrlTemplate: "/welcome",
            defaultIconUrl: "/icons/welcome.png", defaultPriority: NotificationPriority.High,
            tenantId: null, category: "onboarding", supportedPlaceholders: "[\"name\"]");

        template.Id.Should().NotBeEmpty();
        template.Code.Should().Be("welcome_email");
        template.Name.Should().Be("Welcome");
        template.Type.Should().Be(NotificationType.Onboarding);
        template.Channel.Should().Be(NotificationChannel.Email);
        template.TitleTemplate.Should().Be("Welcome {{name}}");
        template.MessageTemplate.Should().Be("Hello {{name}}!");
        template.Description.Should().Be("Welcome template");
        template.ActionUrlTemplate.Should().Be("/welcome");
        template.DefaultIconUrl.Should().Be("/icons/welcome.png");
        template.DefaultPriority.Should().Be(NotificationPriority.High);
        template.IsActive.Should().BeTrue();
        template.Category.Should().Be("onboarding");
        template.SupportedPlaceholders.Should().Be("[\"name\"]");
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldUseDefaults()
    {
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Title", "Message");

        template.DefaultPriority.Should().Be(NotificationPriority.Normal);
        template.IsActive.Should().BeTrue();
        template.Description.Should().BeNull();
        template.ActionUrlTemplate.Should().BeNull();
        template.DefaultIconUrl.Should().BeNull();
        template.Category.Should().BeNull();
    }

    [Fact]
    public void UpdateContent_ShouldUpdateTemplateFields()
    {
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Old Title", "Old Message");

        template.UpdateContent("New Title", "New Message", "/new-action", "/new-icon.png");

        template.TitleTemplate.Should().Be("New Title");
        template.MessageTemplate.Should().Be("New Message");
        template.ActionUrlTemplate.Should().Be("/new-action");
        template.DefaultIconUrl.Should().Be("/new-icon.png");
    }

    [Fact]
    public void UpdateMetadata_ShouldUpdateFields()
    {
        var template = NotificationTemplate.Create(
            "code", "OldName", NotificationType.System,
            NotificationChannel.InApp, "Title", "Msg");

        template.UpdateMetadata("NewName", "Desc", "cat", NotificationPriority.Urgent);

        template.Name.Should().Be("NewName");
        template.Description.Should().Be("Desc");
        template.Category.Should().Be("cat");
        template.DefaultPriority.Should().Be(NotificationPriority.Urgent);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "T", "M");
        template.Deactivate();
        template.IsActive.Should().BeFalse();

        template.Activate();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "T", "M");

        template.Deactivate();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldCallSoftDelete()
    {
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "T", "M");
        // Set Version > 0 to allow SoftDelete
        template.GetType().BaseType!.BaseType!.GetProperty("Version")!.SetValue(template, 1);

        template.Delete();
        template.IsDeleted.Should().BeTrue();
    }
}

/// <summary>
/// Tests for NotificationPreference entity methods.
/// </summary>
public class NotificationPreferenceEntityExtendedTests
{
    [Fact]
    public void UpdateChannelPreferences_ShouldUpdateAllChannels()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());

        pref.UpdateChannelPreferences(false, false, false, true);

        pref.EmailEnabled.Should().BeFalse();
        pref.PushEnabled.Should().BeFalse();
        pref.InAppEnabled.Should().BeFalse();
        pref.SmsEnabled.Should().BeTrue();
    }

    [Fact]
    public void UpdateCategoryPreferences_ShouldUpdateAll()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());

        pref.UpdateCategoryPreferences(false, false, false, false);

        pref.MarketingEnabled.Should().BeFalse();
        pref.SocialEnabled.Should().BeFalse();
        pref.LearningEnabled.Should().BeFalse();
        pref.AchievementsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetQuietHours_ShouldSetStartEndTimezone()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());
        var start = new TimeOnly(22, 0);
        var end = new TimeOnly(7, 0);

        pref.SetQuietHours(start, end, "America/New_York", NotificationPriority.Urgent);

        pref.QuietHoursStart.Should().Be(start);
        pref.QuietHoursEnd.Should().Be(end);
        pref.Timezone.Should().Be("America/New_York");
        pref.QuietHoursBypassPriority.Should().Be(NotificationPriority.Urgent);
    }

    [Fact]
    public void ClearQuietHours_ShouldNullifyFields()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());
        pref.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC");
        pref.ClearQuietHours();

        pref.QuietHoursStart.Should().BeNull();
        pref.QuietHoursEnd.Should().BeNull();
        // Timezone is preserved after ClearQuietHours
    }

    [Fact]
    public void SetEmailDigestFrequency_ShouldSetValue()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());
        pref.SetEmailDigestFrequency(DigestFrequency.Weekly);

        pref.EmailDigestFrequency.Should().Be(DigestFrequency.Weekly);
    }

    [Fact]
    public void SetEmailDigestFrequency_Null_ShouldClear()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());
        pref.SetEmailDigestFrequency(DigestFrequency.Daily);
        pref.SetEmailDigestFrequency(null);

        pref.EmailDigestFrequency.Should().BeNull();
    }

    [Fact]
    public void SetMutedTypes_ShouldSetValue()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());
        pref.SetMutedTypes("[\"Marketing\", \"Social\"]");

        pref.MutedTypes.Should().Be("[\"Marketing\", \"Social\"]");
    }

    [Fact]
    public void SetMutedTypes_Null_ShouldClear()
    {
        var pref = NotificationPreference.CreateDefault(Guid.NewGuid());
        pref.SetMutedTypes("[\"a\"]");
        pref.SetMutedTypes(null);

        pref.MutedTypes.Should().BeNull();
    }
}

/// <summary>
/// Tests for Notification entity edge cases.
/// </summary>
public class NotificationEntityEdgeCaseTests
{
    [Fact]
    public void Delete_ShouldCallSoftDelete()
    {
        var n = Notification.Create(
            Guid.NewGuid(), NotificationType.System,
            NotificationChannel.InApp, "Title", "Message");
        n.GetType().BaseType!.BaseType!.GetProperty("Version")!.SetValue(n, 1);

        n.Delete();
        n.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Create_WithAllOptionalParams_ShouldSetThem()
    {
        var recipientId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var refId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(1);

        var n = Notification.Create(
            recipientId, NotificationType.Security, NotificationChannel.Email,
            "Security Alert", "Your account was accessed",
            tenantId: tenantId, actionUrl: "/security",
            iconUrl: "/icons/security.png",
            priority: NotificationPriority.Urgent,
            referenceEntityId: refId, referenceEntityType: "User",
            metadata: "{\"ip\":\"1.2.3.4\"}",
            templateId: templateId, scheduledAt: scheduledAt);

        n.RecipientId.Should().Be(recipientId);
        n.Type.Should().Be(NotificationType.Security);
        n.Channel.Should().Be(NotificationChannel.Email);
        n.ActionUrl.Should().Be("/security");
        n.IconUrl.Should().Be("/icons/security.png");
        n.Priority.Should().Be(NotificationPriority.Urgent);
        n.ReferenceEntityId.Should().Be(refId);
        n.ReferenceEntityType.Should().Be("User");
        n.Metadata.Should().Contain("ip");
        n.TemplateId.Should().Be(templateId);
        n.ScheduledAt.Should().Be(scheduledAt);
    }
}

/// <summary>
/// Tests for notification enums.
/// </summary>
public class NotificationEnumsTests
{
    [Theory]
    [InlineData(NotificationType.System, 0)]
    [InlineData(NotificationType.CourseEnrollment, 1)]
    [InlineData(NotificationType.CourseCompletion, 2)]
    [InlineData(NotificationType.AchievementUnlocked, 3)]
    [InlineData(NotificationType.CertificateIssued, 4)]
    [InlineData(NotificationType.NewContent, 5)]
    [InlineData(NotificationType.Security, 12)]
    [InlineData(NotificationType.Marketing, 13)]
    [InlineData(NotificationType.Custom, 99)]
    public void NotificationType_ShouldHaveCorrectValues(NotificationType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    [Theory]
    [InlineData(NotificationChannel.InApp, 0)]
    [InlineData(NotificationChannel.Email, 1)]
    [InlineData(NotificationChannel.Push, 2)]
    [InlineData(NotificationChannel.Sms, 3)]
    [InlineData(NotificationChannel.Slack, 4)]
    [InlineData(NotificationChannel.Discord, 5)]
    [InlineData(NotificationChannel.Webhook, 6)]
    public void NotificationChannel_ShouldHaveCorrectValues(NotificationChannel ch, int expected)
    {
        ((int)ch).Should().Be(expected);
    }

    [Theory]
    [InlineData(NotificationPriority.Low, 0)]
    [InlineData(NotificationPriority.Normal, 1)]
    [InlineData(NotificationPriority.High, 2)]
    [InlineData(NotificationPriority.Urgent, 3)]
    public void NotificationPriority_ShouldHaveCorrectValues(NotificationPriority p, int expected)
    {
        ((int)p).Should().Be(expected);
    }

    [Theory]
    [InlineData(DigestFrequency.Daily, 0)]
    [InlineData(DigestFrequency.Weekly, 1)]
    [InlineData(DigestFrequency.BiWeekly, 2)]
    public void DigestFrequency_ShouldHaveCorrectValues(DigestFrequency f, int expected)
    {
        ((int)f).Should().Be(expected);
    }
}

/// <summary>
/// Tests that NotificationService facade delegates to sub-services.
/// </summary>
public class NotificationServiceFacadeTests
{
    private readonly Mock<INotificationDeliveryService> _deliveryMock = new();
    private readonly Mock<INotificationPreferenceService> _prefMock = new();
    private readonly Mock<INotificationTemplateService> _templateMock = new();
    private readonly NotificationService _sut;

    public NotificationServiceFacadeTests()
    {
        _sut = new NotificationService(
            _deliveryMock.Object,
            _prefMock.Object,
            _templateMock.Object);
    }

    // ── Delivery delegations ─────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldDelegate()
    {
        var id = Guid.NewGuid();
        var n = Notification.Create(Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, "T", "M");
        _deliveryMock.Setup(s => s.GetByIdAsync(id, default)).ReturnsAsync(Result<Notification>.Success(n));
        await _sut.GetByIdAsync(id);
        _deliveryMock.Verify(s => s.GetByIdAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        _deliveryMock.Setup(s => s.GetUserNotificationsAsync(userId, 0, 20, null, default))
            .Returns(Task.FromResult(Result<IEnumerable<Notification>>.Success(Enumerable.Empty<Notification>())));
        await _sut.GetUserNotificationsAsync(userId);
        _deliveryMock.Verify(s => s.GetUserNotificationsAsync(userId, 0, 20, null, default), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        _deliveryMock.Setup(s => s.GetUnreadCountAsync(userId, default)).ReturnsAsync(Result<int>.Success(5));
        await _sut.GetUnreadCountAsync(userId);
        _deliveryMock.Verify(s => s.GetUnreadCountAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldDelegate()
    {
        var recipientId = Guid.NewGuid();
        var n = Notification.Create(recipientId, NotificationType.System, NotificationChannel.InApp, "T", "M");
        _deliveryMock.Setup(s => s.SendAsync(recipientId, NotificationType.System, "T", "M", NotificationChannel.InApp, null, null, NotificationPriority.Normal, null, null, null, default))
            .ReturnsAsync(Result<Notification>.Success(n));
        await _sut.SendAsync(recipientId, NotificationType.System, "T", "M");
        _deliveryMock.Verify(s => s.SendAsync(recipientId, NotificationType.System, "T", "M", NotificationChannel.InApp, null, null, NotificationPriority.Normal, null, null, null, default), Times.Once);
    }

    [Fact]
    public async Task SendFromTemplateAsync_ShouldDelegate()
    {
        var recipientId = Guid.NewGuid();
        var placeholders = new Dictionary<string, string> { { "name", "John" } };
        var n = Notification.Create(recipientId, NotificationType.System, NotificationChannel.InApp, "T", "M");
        _deliveryMock.Setup(s => s.SendFromTemplateAsync(recipientId, "welcome", placeholders, null, null, null, default))
            .ReturnsAsync(Result<Notification>.Success(n));
        await _sut.SendFromTemplateAsync(recipientId, "welcome", placeholders);
        _deliveryMock.Verify(s => s.SendFromTemplateAsync(recipientId, "welcome", placeholders, null, null, null, default), Times.Once);
    }

    [Fact]
    public async Task SendBulkAsync_ShouldDelegate()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        _deliveryMock.Setup(s => s.SendBulkAsync(ids, NotificationType.System, "T", "M", NotificationChannel.InApp, null, null, NotificationPriority.Normal, default))
            .Returns(Task.FromResult(Result<IEnumerable<Notification>>.Success(Enumerable.Empty<Notification>())));
        await _sut.SendBulkAsync(ids, NotificationType.System, "T", "M");
        _deliveryMock.Verify(s => s.SendBulkAsync(ids, NotificationType.System, "T", "M", NotificationChannel.InApp, null, null, NotificationPriority.Normal, default), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldDelegate()
    {
        var recipientId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(1);
        var n = Notification.Create(recipientId, NotificationType.System, NotificationChannel.InApp, "T", "M");
        _deliveryMock.Setup(s => s.ScheduleAsync(recipientId, NotificationType.System, "T", "M", scheduledAt, NotificationChannel.InApp, null, null, NotificationPriority.Normal, default))
            .ReturnsAsync(Result<Notification>.Success(n));
        await _sut.ScheduleAsync(recipientId, NotificationType.System, "T", "M", scheduledAt);
        _deliveryMock.Verify(s => s.ScheduleAsync(recipientId, NotificationType.System, "T", "M", scheduledAt, NotificationChannel.InApp, null, null, NotificationPriority.Normal, default), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldDelegate()
    {
        var id = Guid.NewGuid();
        _deliveryMock.Setup(s => s.MarkAsReadAsync(id, default)).ReturnsAsync(Result.Success());
        await _sut.MarkAsReadAsync(id);
        _deliveryMock.Verify(s => s.MarkAsReadAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        _deliveryMock.Setup(s => s.MarkAllAsReadAsync(userId, default)).ReturnsAsync(Result.Success());
        await _sut.MarkAllAsReadAsync(userId);
        _deliveryMock.Verify(s => s.MarkAllAsReadAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task MarkAsUnreadAsync_ShouldDelegate()
    {
        var id = Guid.NewGuid();
        _deliveryMock.Setup(s => s.MarkAsUnreadAsync(id, default)).ReturnsAsync(Result.Success());
        await _sut.MarkAsUnreadAsync(id);
        _deliveryMock.Verify(s => s.MarkAsUnreadAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDelegate()
    {
        var id = Guid.NewGuid();
        _deliveryMock.Setup(s => s.DeleteAsync(id, default)).ReturnsAsync(Result.Success());
        await _sut.DeleteAsync(id);
        _deliveryMock.Verify(s => s.DeleteAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task DeleteReadNotificationsAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        _deliveryMock.Setup(s => s.DeleteReadNotificationsAsync(userId, default)).ReturnsAsync(Result<int>.Success(3));
        await _sut.DeleteReadNotificationsAsync(userId);
        _deliveryMock.Verify(s => s.DeleteReadNotificationsAsync(userId, default), Times.Once);
    }

    // ── Preference delegations ───────────────────────────────────

    [Fact]
    public async Task GetPreferencesAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        _prefMock.Setup(s => s.GetPreferencesAsync(userId, default)).ReturnsAsync(Result<NotificationPreference>.Success(NotificationPreference.CreateDefault(userId)));
        await _sut.GetPreferencesAsync(userId);
        _prefMock.Verify(s => s.GetPreferencesAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        _prefMock.Setup(s => s.UpdatePreferencesAsync(userId, true, null, null, null, null, null, null, null, default))
            .ReturnsAsync(Result<NotificationPreference>.Success(NotificationPreference.CreateDefault(userId)));
        await _sut.UpdatePreferencesAsync(userId, emailEnabled: true);
        _prefMock.Verify(s => s.UpdatePreferencesAsync(userId, true, null, null, null, null, null, null, null, default), Times.Once);
    }

    [Fact]
    public async Task SetQuietHoursAsync_ShouldDelegate()
    {
        var userId = Guid.NewGuid();
        var start = new TimeOnly(22, 0);
        var end = new TimeOnly(7, 0);
        _prefMock.Setup(s => s.SetQuietHoursAsync(userId, start, end, "UTC", default)).ReturnsAsync(Result.Success());
        await _sut.SetQuietHoursAsync(userId, start, end, "UTC");
        _prefMock.Verify(s => s.SetQuietHoursAsync(userId, start, end, "UTC", default), Times.Once);
    }

    // ── Template delegations ─────────────────────────────────────

    [Fact]
    public async Task GetTemplateByCodeAsync_ShouldDelegate()
    {
        var template = NotificationTemplate.Create("code", "Name", NotificationType.System, NotificationChannel.InApp, "T", "M");
        _templateMock.Setup(s => s.GetTemplateByCodeAsync("code", default)).ReturnsAsync(Result<NotificationTemplate>.Success(template));
        await _sut.GetTemplateByCodeAsync("code");
        _templateMock.Verify(s => s.GetTemplateByCodeAsync("code", default), Times.Once);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldDelegate()
    {
        _templateMock.Setup(s => s.GetTemplatesAsync(null, null, default))
            .Returns(Task.FromResult(Result<IEnumerable<NotificationTemplate>>.Success(Enumerable.Empty<NotificationTemplate>())));
        await _sut.GetTemplatesAsync();
        _templateMock.Verify(s => s.GetTemplatesAsync(null, null, default), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldDelegate()
    {
        var template = NotificationTemplate.Create("c", "N", NotificationType.System, NotificationChannel.InApp, "T", "M");
        _templateMock.Setup(s => s.CreateTemplateAsync("c", "N", NotificationType.System, NotificationChannel.InApp, "T", "M", null, null, null, default))
            .ReturnsAsync(Result<NotificationTemplate>.Success(template));
        await _sut.CreateTemplateAsync("c", "N", NotificationType.System, NotificationChannel.InApp, "T", "M");
        _templateMock.Verify(s => s.CreateTemplateAsync("c", "N", NotificationType.System, NotificationChannel.InApp, "T", "M", null, null, null, default), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldDelegate()
    {
        var id = Guid.NewGuid();
        var template = NotificationTemplate.Create("c", "N", NotificationType.System, NotificationChannel.InApp, "T", "M");
        _templateMock.Setup(s => s.UpdateTemplateAsync(id, "NewT", null, null, null, default))
            .ReturnsAsync(Result<NotificationTemplate>.Success(template));
        await _sut.UpdateTemplateAsync(id, titleTemplate: "NewT");
        _templateMock.Verify(s => s.UpdateTemplateAsync(id, "NewT", null, null, null, default), Times.Once);
    }
}

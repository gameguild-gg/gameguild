namespace GameGuild.Notifications.UnitTests.Entities;

public class NotificationTests
{
    [Fact]
    public void Create_Should_Initialize_All_Fields()
    {
        var tenantId = Guid.NewGuid();
        var scheduledAt = SystemClock.UtcNow.AddHours(2);
        var referenceEntityId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Billing,
            NotificationChannel.Email,
            "Invoice ready",
            "Your invoice is ready",
            tenantId,
            "https://example.test/invoices/1",
            "https://example.test/icon.svg",
            NotificationPriority.High,
            scheduledAt,
            referenceEntityId,
            "Invoice",
            "{\"amount\":42}",
            templateId);

        notification.Id.Should().NotBe(Guid.Empty);
        notification.NotificationTenantId.Should().NotBeNull();
        notification.NotificationTenantId!.Value.Value.Should().Be(tenantId);
        notification.Type.Should().Be(NotificationType.Billing);
        notification.Channel.Should().Be(NotificationChannel.Email);
        notification.Title.Should().Be("Invoice ready");
        notification.Message.Should().Be("Your invoice is ready");
        notification.ActionUrl.Should().Be("https://example.test/invoices/1");
        notification.IconUrl.Should().Be("https://example.test/icon.svg");
        notification.Priority.Should().Be(NotificationPriority.High);
        notification.ScheduledAt.Should().Be(scheduledAt);
        notification.ReferenceEntityId.Should().Be(referenceEntityId);
        notification.ReferenceEntityType.Should().Be("Invoice");
        notification.Metadata.Should().Be("{\"amount\":42}");
        notification.TemplateId.Should().Be(templateId);
        notification.IsRead.Should().BeFalse();
        notification.IsSent.Should().BeFalse();
    }

    [Fact]
    public void MarkAsRead_And_MarkAsUnread_Should_Update_Read_State()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.System,
            NotificationChannel.InApp,
            "Welcome",
            "Hello");

        notification.MarkAsRead();
        var readAt = notification.ReadAt;

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();

        notification.MarkAsRead();

        notification.ReadAt.Should().Be(readAt);

        notification.MarkAsUnread();

        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsSent_Should_Set_Sent_State_Only_Once()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.System,
            NotificationChannel.InApp,
            "Welcome",
            "Hello");

        notification.MarkAsSent();
        var sentAt = notification.SentAt;

        notification.IsSent.Should().BeTrue();
        notification.SentAt.Should().NotBeNull();

        notification.MarkAsSent();

        notification.SentAt.Should().Be(sentAt);
    }

    [Fact]
    public void Delete_Should_SoftDelete_Persisted_Notification()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.System,
            NotificationChannel.InApp,
            "Welcome",
            "Hello");

        notification.Version = 1;

        notification.Delete();

        notification.IsDeleted.Should().BeTrue();
        notification.DeletedAt.Should().NotBeNull();
    }
}

namespace GameGuild.Notifications.UnitTests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task Crud_Methods_Should_Delegate_To_Delivery_Service()
    {
        var recipientId = Guid.NewGuid();
        var notification = Notification.Create(recipientId, NotificationType.System, NotificationChannel.InApp, "Title", "Message");
        var notifications = new[] { notification };
        var delivery = new Mock<INotificationDeliveryService>(MockBehavior.Strict);
        var preference = new Mock<INotificationPreferenceService>(MockBehavior.Strict);
        var template = new Mock<INotificationTemplateService>(MockBehavior.Strict);
        var subject = new NotificationService(delivery.Object, preference.Object, template.Object);

        delivery.Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(notification));
        delivery.Setup(x => x.GetUserNotificationsAsync(recipientId, 2, 5, true, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<IEnumerable<Notification>>(notifications));
        delivery.Setup(x => x.GetUnreadCountAsync(recipientId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(3));
        delivery.Setup(x => x.SendAsync(recipientId, NotificationType.System, "Title", "Message", NotificationChannel.Email, null, "https://example.test", NotificationPriority.High, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(notification));
        delivery.Setup(x => x.SendFromTemplateAsync(recipientId, "welcome", It.IsAny<Dictionary<string, string>>(), null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(notification));
        delivery.Setup(x => x.SendBulkAsync(It.IsAny<IEnumerable<Guid>>(), NotificationType.System, "Bulk", "Message", NotificationChannel.InApp, null, null, NotificationPriority.Normal, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<IEnumerable<Notification>>(notifications));
        delivery.Setup(x => x.ScheduleAsync(recipientId, NotificationType.System, "Later", "Message", It.IsAny<DateTime>(), NotificationChannel.InApp, null, null, NotificationPriority.Normal, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(notification));

        (await subject.GetByIdAsync(notification.Id)).Value.Should().BeSameAs(notification);
        (await subject.GetUserNotificationsAsync(recipientId, 2, 5, true)).Value.Should().BeEquivalentTo(notifications);
        (await subject.GetUnreadCountAsync(recipientId)).Value.Should().Be(3);
        (await subject.SendAsync(recipientId, NotificationType.System, "Title", "Message", NotificationChannel.Email, actionUrl: "https://example.test", priority: NotificationPriority.High)).Value.Should().BeSameAs(notification);
        (await subject.SendFromTemplateAsync(recipientId, "welcome", new Dictionary<string, string> { ["name"] = "Ada" })).Value.Should().BeSameAs(notification);
        (await subject.SendBulkAsync([recipientId], NotificationType.System, "Bulk", "Message")).Value.Should().BeEquivalentTo(notifications);
        (await subject.ScheduleAsync(recipientId, NotificationType.System, "Later", "Message", SystemClock.UtcNow.AddMinutes(5))).Value.Should().BeSameAs(notification);

        delivery.VerifyAll();
    }

    [Fact]
    public async Task Status_Methods_Should_Delegate_To_Delivery_Service()
    {
        var notificationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var delivery = new Mock<INotificationDeliveryService>(MockBehavior.Strict);
        var preference = new Mock<INotificationPreferenceService>(MockBehavior.Strict);
        var template = new Mock<INotificationTemplateService>(MockBehavior.Strict);
        var subject = new NotificationService(delivery.Object, preference.Object, template.Object);

        delivery.Setup(x => x.MarkAsReadAsync(notificationId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        delivery.Setup(x => x.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        delivery.Setup(x => x.MarkAsUnreadAsync(notificationId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        delivery.Setup(x => x.DeleteAsync(notificationId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        delivery.Setup(x => x.DeleteReadNotificationsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(2));

        (await subject.MarkAsReadAsync(notificationId)).IsSuccess.Should().BeTrue();
        (await subject.MarkAllAsReadAsync(userId)).IsSuccess.Should().BeTrue();
        (await subject.MarkAsUnreadAsync(notificationId)).IsSuccess.Should().BeTrue();
        (await subject.DeleteAsync(notificationId)).IsSuccess.Should().BeTrue();
        (await subject.DeleteReadNotificationsAsync(userId)).Value.Should().Be(2);

        delivery.VerifyAll();
    }

    [Fact]
    public async Task Preference_Methods_Should_Delegate_To_Preference_Service()
    {
        var userId = Guid.NewGuid();
        var preferenceValue = NotificationPreference.CreateDefault(userId);
        var delivery = new Mock<INotificationDeliveryService>(MockBehavior.Strict);
        var preference = new Mock<INotificationPreferenceService>(MockBehavior.Strict);
        var template = new Mock<INotificationTemplateService>(MockBehavior.Strict);
        var subject = new NotificationService(delivery.Object, preference.Object, template.Object);

        preference.Setup(x => x.GetPreferencesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(preferenceValue));
        preference.Setup(x => x.UpdatePreferencesAsync(userId, false, true, false, true, false, true, false, true, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(preferenceValue));
        preference.Setup(x => x.SetQuietHoursAsync(userId, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        (await subject.GetPreferencesAsync(userId)).Value.Should().BeSameAs(preferenceValue);
        (await subject.UpdatePreferencesAsync(userId, false, true, false, true, false, true, false, true)).Value.Should().BeSameAs(preferenceValue);
        (await subject.SetQuietHoursAsync(userId, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC")).IsSuccess.Should().BeTrue();

        preference.VerifyAll();
    }

    [Fact]
    public async Task Template_Methods_Should_Delegate_To_Template_Service()
    {
        var templateValue = NotificationTemplate.Create(
            "welcome",
            "Welcome",
            NotificationType.Onboarding,
            NotificationChannel.Email,
            "Welcome {{name}}",
            "Hello {{name}}");
        var delivery = new Mock<INotificationDeliveryService>(MockBehavior.Strict);
        var preference = new Mock<INotificationPreferenceService>(MockBehavior.Strict);
        var template = new Mock<INotificationTemplateService>(MockBehavior.Strict);
        var subject = new NotificationService(delivery.Object, preference.Object, template.Object);

        template.Setup(x => x.GetTemplateByCodeAsync("welcome", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(templateValue));
        template.Setup(x => x.GetTemplatesAsync("Onboarding", true, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<IEnumerable<NotificationTemplate>>([templateValue]));
        template.Setup(x => x.CreateTemplateAsync("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome {{name}}", "Hello {{name}}", "desc", "https://example.test", "Onboarding", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(templateValue));
        template.Setup(x => x.UpdateTemplateAsync(templateValue.Id, "Hi {{name}}", "Updated", "https://example.test/updated", false, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(templateValue));

        (await subject.GetTemplateByCodeAsync("welcome")).Value.Should().BeSameAs(templateValue);
        (await subject.GetTemplatesAsync("Onboarding", true)).Value.Should().ContainSingle();
        (await subject.CreateTemplateAsync("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome {{name}}", "Hello {{name}}", "desc", "https://example.test", "Onboarding")).Value.Should().BeSameAs(templateValue);
        (await subject.UpdateTemplateAsync(templateValue.Id, "Hi {{name}}", "Updated", "https://example.test/updated", false)).Value.Should().BeSameAs(templateValue);

        template.VerifyAll();
    }
}

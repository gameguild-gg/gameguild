namespace GameGuild.Notifications.UnitTests;

public class NotificationsModuleTests
{
    [Fact]
    public void AddNotificationsModule_Should_Register_All_Module_Services()
    {
        var services = new ServiceCollection();

        var returned = services.AddNotificationsModule();

        returned.Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(INotificationPreferenceService) &&
            descriptor.ImplementationType == typeof(NotificationPreferenceService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(INotificationTemplateService) &&
            descriptor.ImplementationType == typeof(NotificationTemplateService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(INotificationDeliveryService) &&
            descriptor.ImplementationType == typeof(NotificationDeliveryService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(INotificationService) &&
            descriptor.ImplementationType == typeof(NotificationService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IApplicationNotificationPublisher) &&
            descriptor.ImplementationType == typeof(ApplicationNotificationPublisher) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task ApplicationNotificationPublisher_ShouldDelegateToNotificationService()
    {
        var notificationId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = Notification.Create(
            recipientId,
            NotificationType.Custom,
            NotificationChannel.InApp,
            "Quota exceeded",
            "Storage quota exceeded",
            tenantId,
            priority: NotificationPriority.High);
        notification.Id = notificationId;
        var service = new Mock<INotificationService>();
        service
            .Setup(x => x.SendAsync(
                recipientId,
                NotificationType.Custom,
                "Quota exceeded",
                "Storage quota exceeded",
                NotificationChannel.InApp,
                tenantId,
                "/resources/quotas",
                NotificationPriority.High,
                null,
                "ResourceQuota",
                It.Is<string>(metadata => metadata.Contains("Storage")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notification));
        var publisher = new ApplicationNotificationPublisher(service.Object);

        var result = await publisher.PublishAsync(
            new ApplicationNotificationMessage(
                recipientId,
                "Quota exceeded",
                "Storage quota exceeded",
                "ResourceQuotaExceeded",
                "High",
                tenantId,
                "/resources/quotas",
                null,
                "ResourceQuota",
                new Dictionary<string, string> { ["resourceType"] = "Storage" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NotificationId.Should().Be(notificationId);
    }

    [Fact]
    public async Task ApplicationNotificationPublisher_ShouldUseParsedTypeAndDefaultPriority()
    {
        var notificationId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var notification = Notification.Create(
            recipientId,
            NotificationType.Security,
            NotificationChannel.InApp,
            "New session",
            "A new console session was started",
            priority: NotificationPriority.Normal);
        notification.Id = notificationId;
        var service = new Mock<INotificationService>();
        service
            .Setup(x => x.SendAsync(
                recipientId,
                NotificationType.Security,
                "New session",
                "A new console session was started",
                NotificationChannel.InApp,
                null,
                null,
                NotificationPriority.Normal,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notification));
        var publisher = new ApplicationNotificationPublisher(service.Object);

        var result = await publisher.PublishAsync(
            new ApplicationNotificationMessage(
                recipientId,
                "New session",
                "A new console session was started",
                "Security",
                "UnexpectedPriority"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NotificationId.Should().Be(notificationId);
    }
}

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
    }
}

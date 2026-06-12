using GameGuild.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Notifications;

/// <summary>
/// Module registration for Notification services
/// </summary>
public static class NotificationsModule
{
    /// <summary>
    /// Adds Notifications module services to the DI container
    /// </summary>
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        // Sub-services (focused, single-responsibility)
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();

        // Facade for backward compatibility
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IApplicationNotificationPublisher, ApplicationNotificationPublisher>();

        return services;
    }
}

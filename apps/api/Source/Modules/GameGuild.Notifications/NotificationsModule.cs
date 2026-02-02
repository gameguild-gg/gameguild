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
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

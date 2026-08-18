using GameGuild.Notifications.Services;
using GameGuild.Notifications.Services.Email;
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

        // Email dispatch pipeline
        services.AddOptions<EmailDispatcherOptions>()
            .BindConfiguration("Notifications:EmailDispatcher");
        services.AddScoped<IEmailRendererRegistry, EmailRendererRegistry>();
        services.AddScoped<IRecipientEmailResolver, RecipientEmailResolver>();
        services.AddScoped<EmailDispatcherService>();
        services.AddHostedService<EmailDispatcherBackgroundService>();

        // One-click unsubscribe tokens (IDataProtectionProvider is registered by the API host)
        services.AddScoped<IUnsubscribeTokenService, UnsubscribeTokenService>();

        // Footer injection for suppressible emails (consumed by renderers via constructor injection)
        services.AddScoped<IEmailFooterService, EmailFooterService>();

        // Facade for backward compatibility
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

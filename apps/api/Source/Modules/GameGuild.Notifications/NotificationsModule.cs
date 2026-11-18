using GameGuild.Notifications.Services;
using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.Services.Email.Renderers;
using GameGuild.Notifications.Services.Email.Handlers;
using GameGuild.Notifications.Services.Push;
using GameGuild.Notifications.Services.Sms;
using GameGuild.Notifications.Services.WhatsApp;
using GameGuild.Identity.Tenants;
using GameGuild.Push;
using GameGuild.Sms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        services.AddScoped<NotificationMetadataProtector>();

        // Email dispatch pipeline
        services.AddOptions<EmailDispatcherOptions>()
            .BindConfiguration("Notifications:EmailDispatcher");
        services.AddScoped<IEmailRendererRegistry, EmailRendererRegistry>();
        services.AddScoped<IEmailRenderer, SecurityEmailRenderer>();
        // Ordinary-type renderer family (issue #1319 N2): one renderer per otherwise
        // renderer-less ordinary type, so an Email-channel row of those types renders from
        // its Title/Message payload instead of dead-lettering with "No email renderer
        // registered". Dedicated renderers registered by producer modules keep precedence
        // (the registry enforces one renderer per type; these types have none).
        foreach (var ordinaryType in OrdinaryNotificationEmailRenderer.CoveredTypes)
        {
            services.AddScoped<IEmailRenderer>(serviceProvider => new OrdinaryNotificationEmailRenderer(
                ordinaryType,
                serviceProvider.GetRequiredService<IEmailFooterService>()));
        }

        services.AddScoped<IRecipientEmailResolver, RecipientEmailResolver>();
        services.AddScoped<EmailDispatcherService>();
        services.AddHostedService<EmailDispatcherBackgroundService>();

        // Email digest engine (daily/weekly/biweekly bundled delivery)
        services.AddOptions<DigestDispatcherOptions>()
            .BindConfiguration("Notifications:DigestDispatcher");
        services.AddScoped<DigestRenderer>();
        services.AddScoped<DigestDispatcherService>();
        services.AddHostedService<DigestDispatcherBackgroundService>();

        // SMS dispatch pipeline (delivery port ISmsSender is registered by the API host; the
        // module keeps only the no-op fallback so isolated module tests never need a provider).
        services.TryAddSingleton<ISmsSender>(static _ => NoOpSmsSender.Instance);
        services.AddOptions<SmsDispatcherOptions>()
            .BindConfiguration("Notifications:SmsDispatcher");
        services.AddScoped<SmsDispatcherService>();
        services.AddHostedService<SmsDispatcherBackgroundService>();

        // Mobile-push dispatch pipeline (delivery port IPushSender likewise host-provided).
        services.TryAddSingleton<IPushSender>(static _ => NoOpPushSender.Instance);
        services.AddOptions<PushDispatcherOptions>()
            .BindConfiguration("Notifications:PushDispatcher");
        services.AddScoped<PushDispatcherService>();
        services.AddHostedService<PushDispatcherBackgroundService>();

        // WhatsApp dispatch pipeline (issues #54/#55): config-gated port with a fail-closed
        // NoOp default (same pattern as the Plaid instant-verification seam). The real
        // Cloud API adapter is registered by the API host once environment credentials
        // (WHATSAPP_APP_ID / WHATSAPP_PHONE_NUMBER_ID / WHATSAPP_ACCESS_TOKEN) exist and
        // Notifications:WhatsApp:Enabled is true; until then every send is declined
        // observably and broadcasts fail explicitly.
        services.AddSingleton<IValidateOptions<WhatsAppSettings>, WhatsAppSettingsValidator>();
        services.AddOptions<WhatsAppSettings>()
            .BindConfiguration(WhatsAppSettings.SectionName)
            .ValidateOnStart();
        services.TryAddSingleton<IWhatsAppProvider, NoOpWhatsAppProvider>();
        services.AddOptions<WhatsAppDispatcherOptions>()
            .BindConfiguration("Notifications:WhatsApp:Dispatcher");
        services.AddScoped<IWhatsAppRecipientPhoneResolver, WhatsAppRecipientPhoneResolver>();
        services.AddScoped<WhatsAppDispatcherService>();
        services.AddHostedService<WhatsAppDispatcherBackgroundService>();

        // Broadcast campaign engine (issue #55): tenant-scoped template campaigns with a
        // bounded recipient cap, opt-in filtering and per-recipient delivery tracking.
        services.AddOptions<WhatsAppBroadcastOptions>()
            .BindConfiguration(WhatsAppBroadcastOptions.SectionName);
        services.AddScoped<IWhatsAppBroadcastAudienceResolver, WhatsAppBroadcastAudienceResolver>();
        services.AddScoped<IWhatsAppBroadcastService, WhatsAppBroadcastService>();
        services.AddHostedService<WhatsAppBroadcastSchedulerBackgroundService>();

        // Tenant invite renderer. Registered here (not in the producing Identity.Tenants module) because
        // Identity.Tenants cannot reference GameGuild.Notifications (circular: Tenants -> Notifications -> Users -> Tenants).
        services.AddScoped<IEmailRenderer, TenantInviteRenderer>();
        services.AddScoped<IIntegrationEventHandler<TenantMemberInviteRequestedV1>, TenantInviteRequestedHandler>();

        // One-click unsubscribe tokens (IDataProtectionProvider is registered by the API host)
        services.AddScoped<IUnsubscribeTokenService, UnsubscribeTokenService>();

        // Footer injection for suppressible emails (consumed by renderers via constructor injection)
        services.AddScoped<IEmailFooterService, EmailFooterService>();

        // Platform-level deliverability administration (event feed, suppressions, requeue, timeline)
        services.AddScoped<IEmailDeliveryAdminService, EmailDeliveryAdminService>();

        // SES/SNS email-events webhook: signature verification + inline suppression processing
        // (the webhook command owns the single transactional SaveChanges for both)
        services.AddScoped<ISnsMessageVerifier, SnsMessageVerifier>();
        services.AddScoped<IEmailEventProcessor, EmailEventProcessor>();
        services.AddHttpClient(Controllers.EmailEventsController.SubscriptionConfirmationClientName, client =>
            client.Timeout = TimeSpan.FromSeconds(15));

        // Facade for backward compatibility
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

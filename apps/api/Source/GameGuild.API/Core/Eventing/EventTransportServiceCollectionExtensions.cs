using GameGuild.API.Core.Billing;
using GameGuild.API.Core.Security;
using GameGuild.Commerce.Billing;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.Eventing;

internal static class EventTransportServiceCollectionExtensions
{
    public static IServiceCollection AddDurableEventTransport(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUseCaseOperationContextAccessor, UseCaseOperationContextAccessor>();
        services.AddSingleton<IUseCaseEventContractRegistry, UseCaseEventContractRegistry>();
        services.AddScoped<IUseCaseEventVerifier, UseCaseEventVerifier>();
        services.AddScoped<IDurableEventProducer, DurableEventProducer>();
        services.AddScoped<IIntegrationEventHandler<RefreshTokenReplayContainedV1>, RefreshTokenReplayAlertHandler>();
        services.AddScoped<IIntegrationEventHandler<SuspiciousLoginDetectedV1>, SuspiciousLoginAlertHandler>();
        services.AddScoped<IIntegrationEventHandler<SuspiciousLoginDetectedV1>, RiskEventSessionRevocationHandler>();
        // Issue #1083: a fresh account lockout emails the owner with the reason, the
        // unlock timeline and a support contact.
        services.AddScoped<IIntegrationEventHandler<AccountLockoutDetectedV1>, AccountLockoutAlertHandler>();
        // Issue #1086: sessions evicted by the concurrent session limit notify the owner
        // and point them at the session management surface.
        services.AddScoped<IIntegrationEventHandler<SessionEvictionDetectedV1>, SessionEvictionAlertHandler>();
        // Issue #1085: a sign-in from a first-seen device fingerprint alerts the owner
        // with the device summary, coarse location and a path to terminate the session.
        services.AddScoped<IIntegrationEventHandler<NewDeviceSignInDetectedV1>, NewDeviceSignInAlertHandler>();
        // Issue #278: ordinary new sign-ins notify the owner only when they opted in
        // (default off; the per-user preference is evaluated host-side, fail-closed).
        services.AddScoped<IIntegrationEventHandler<NewLoginDetectedV1>, NewLoginAlertHandler>();
        // Issue #1091: password change/reset terminated every other session — queue the
        // owner confirmation (InApp always, Email per delivery preferences).
        services.AddScoped<IIntegrationEventHandler<OtherSessionsTerminatedV1>, SessionsTerminatedAlertHandler>();
        // Issue #406: completed provider invoice payments credit the paying user's billing
        // wallet (idempotent per provider event identifier).
        services.AddScoped<IIntegrationEventHandler<BillingInvoicePaidV1>, BillingInvoicePaidWalletCreditHandler>();
        services.AddScoped<IInboxStore, InboxStore>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddScoped<OutboxCleanupService>();
        services.AddScoped<IEventReplayService, EventReplayService>();
        services.AddHostedService<OutboxBackgroundService>();
        return services;
    }
}

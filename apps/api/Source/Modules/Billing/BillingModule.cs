using GameGuild.Modules.Billing.Services;


namespace GameGuild.Modules.Billing;

/// <summary> Extension methods for registering Billing module services </summary>
public static class BillingModule {
    /// <summary> Registers all Billing module services </summary>
    public static IServiceCollection AddBillingModule(this IServiceCollection services) {
        // Register Billing services
        services.AddScoped<IBillingWebhookService, BillingWebhookService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }
}

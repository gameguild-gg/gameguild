using GameGuild.Core.Modules;
using GameGuild.Modules.Billing.Services;

namespace GameGuild.Source.Modules.Billing;

/// <summary>
/// Billing module implementing the standardized IModule interface.
/// Provides comprehensive billing services following Clean Architecture.
/// </summary>
[StandardizedModule("Comprehensive billing services following Clean Architecture")]
[ModuleVersion("1.0.0")]
public class BillingModule : ModuleBase {
    public override string ModuleName => "Billing";
    public override string ModuleVersion => "1.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        base.ConfigureServices(services, configuration);

        // Register Billing services
        services.AddScoped<IBillingWebhookService, BillingWebhookService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app) {
        base.MapEndpoints(app);

        // Billing module doesn't have specific middleware currently
        // This can be extended when needed for billing-specific routes or middleware

        return app;
    }
}

/// <summary>
/// Extension methods for the Billing module providing the standardized pattern.
/// </summary>
public static class BillingModuleExtensions {
    /// <summary>
    /// Registers the Billing module using the IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration) {
        return services.AddModule<BillingModule>(configuration);
    }

    /// <summary>
    /// Maps Billing module endpoints using the IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseBillingModule(this WebApplication app) {
        return app.UseModule<BillingModule>();
    }
}

using GameGuild.Core.Modules;

namespace GameGuild.Source.Modules.Payments;

/// <summary>
/// Payments module implementing the standardized IModule interface.
/// Provides comprehensive payment processing services following Clean Architecture.
/// </summary>
[StandardizedModule("Comprehensive payment processing services following Clean Architecture")]
[ModuleVersion("1.0.0")]
public class PaymentsModule : ModuleBase {
  public override string ModuleName => "Payments";
  public override string ModuleVersion => "1.0.0";

  public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
    base.ConfigureServices(services, configuration);

    // Register Payments services
    // TODO: Add payment gateway services, repositories, and handlers

    // CQRS handlers are automatically registered by assembly scanning

    return services;
  }

  public override WebApplication MapEndpoints(WebApplication app) {
    base.MapEndpoints(app);

    // Payments module doesn't have specific middleware currently
    // This can be extended when needed for payment-specific routes or middleware

    return app;
  }
}

/// <summary>
/// Extension methods for the Payments module providing the standardized pattern.
/// </summary>
public static class PaymentsModuleExtensions {
  /// <summary>
  /// Registers the Payments module using the IModule pattern.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The application configuration</param>
  /// <returns>The service collection for chaining</returns>
  public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration) {
    return services.AddModule<PaymentsModule>(configuration);
  }

  /// <summary>
  /// Maps Payments module endpoints using the IModule pattern.
  /// </summary>
  /// <param name="app">The web application</param>
  /// <returns>The web application for chaining</returns>
  public static WebApplication UsePaymentsModule(this WebApplication app) {
    return app.UseModule<PaymentsModule>();
  }
}

using GameGuild.Core.Modules;

namespace GameGuild.Source.Modules.TestingLab;

/// <summary>
/// TestingLab module implementing the standardized IModule interface.
/// Provides comprehensive testing lab services following Clean Architecture.
/// </summary>
[StandardizedModule("Comprehensive testing lab services following Clean Architecture")]
[ModuleVersion("1.0.0")]
public class TestingLabModule : ModuleBase {
  public override string ModuleName => "TestingLab";
  public override string ModuleVersion => "1.0.0";

  public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
    base.ConfigureServices(services, configuration);

    // Register TestingLab repositories
    // Note: Service registrations will be added based on actual available types
    // services.AddScoped<ITestingRequestRepository, TestingRequestRepository>();
    // services.AddScoped<ITestingLocationRepository, TestingLocationRepository>();

    // Register TestingLab services  
    // services.AddScoped<ITestingRequestService, TestingRequestService>();
    // services.AddScoped<ITestingSessionService, TestingSessionService>();

    // CQRS handlers are automatically registered by assembly scanning

    return services;
  }

  public override WebApplication MapEndpoints(WebApplication app) {
    base.MapEndpoints(app);

    // TestingLab module doesn't have specific middleware currently
    // This can be extended when needed for testing-specific routes or middleware

    return app;
  }
}

/// <summary>
/// Extension methods for the TestingLab module providing the standardized pattern.
/// </summary>
public static class TestingLabModuleExtensions {
  /// <summary>
  /// Registers the TestingLab module using the IModule pattern.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The application configuration</param>
  /// <returns>The service collection for chaining</returns>
  public static IServiceCollection AddTestingLabModule(this IServiceCollection services, IConfiguration configuration) {
    return services.AddModule<TestingLabModule>(configuration);
  }

  /// <summary>
  /// Maps TestingLab module endpoints using the IModule pattern.
  /// </summary>
  /// <param name="app">The web application</param>
  /// <returns>The web application for chaining</returns>
  public static WebApplication UseTestingLabModule(this WebApplication app) {
    return app.UseModule<TestingLabModule>();
  }
}

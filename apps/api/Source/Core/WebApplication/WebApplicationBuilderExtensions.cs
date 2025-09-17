using DotNetEnv;
using GameGuild.CQRS;


namespace GameGuild;

/// <summary> Modern .NET extension methods for WebApplicationBuilder following best practices. Provides fluent configuration with clean separation of concerns. </summary>
public static class WebApplicationBuilderExtensions {
  /// <summary> Configures the WebApplicationBuilder with all GameGuild layers using default options. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The configured WebApplicationBuilder </returns>
  public static WebApplicationBuilder ConfigureWebApplication(this WebApplicationBuilder builder) {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ConfigureEnvironment();

    // Add services by architectural layer with default options
    builder.Services.AddPresentationLayer(builder.Configuration);

    // Add application layer services (CQRS handlers, domain services)
    builder.Services.AddApplicationLayer(builder.Configuration);

    // Add infrastructure layer services (repositories, external services)
    builder.Services.AddInfrastructureLayer(builder.Configuration);

    return builder;
  }

  /// <summary> Configures environment variables and configuration sources. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  public static WebApplicationBuilder ConfigureEnvironment(this WebApplicationBuilder builder) {
    // Load .env file for local development
    Env.Load();

    // Configure configuration sources with proper precedence
    builder.Configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", true, true).AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true).AddEnvironmentVariables(); // Highest precedence

    return builder;
  }

  /// <summary> Configures environment variables and configuration sources. Adds JSON configuration files with proper precedence and reload-on-change support. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  /// <exception cref="ArgumentNullException"> Thrown when the builder is null </exception>
  public static WebApplicationBuilder AddAppSettings(this WebApplicationBuilder builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // Configure configuration sources with proper precedence
    builder // 
      .Configuration // 
      .SetBasePath(AppContext.BaseDirectory) // 
      .AddJsonFile("appsettings.json", true, true) // 
      .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

    return builder;
  }

  /// <summary> Adds environment variables to the configuration pipeline. Supports loading .env files for local development scenarios. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  /// <exception cref="ArgumentNullException"> Thrown when the builder is null </exception>
  public static WebApplicationBuilder AddEnvironmentVariables(this WebApplicationBuilder builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // TODO: Load .env file for local development.
    // Env.Load();
    builder.Configuration.AddEnvironmentVariables();

    return builder;
  }

  /// <summary> Registers services and configurations for the presentation layer. Includes controllers, API versioning, CORS, and other web-specific services. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  /// <exception cref="ArgumentNullException"> Thrown when the builder is null </exception>
  public static WebApplicationBuilder AddPresentationLayer(this WebApplicationBuilder builder) {
    ArgumentNullException.ThrowIfNull(builder);

    builder.Services.AddPresentationLayer(builder.Configuration);

    return builder;
  }

  /// <summary> Adds the presentation layer services to the WebApplicationBuilder with custom options. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <param name="setupPresentationLayerOptions"> Action to configure presentation options </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  /// <exception cref="ArgumentNullException"> Thrown when the builder is null </exception>
  public static WebApplicationBuilder AddPresentationLayer(this WebApplicationBuilder builder, Action<PresentationLayerOptions> setupPresentationLayerOptions) {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(setupPresentationLayerOptions);

    // Create and configure options
    var presentationOptions = PresentationLayerOptionsBuilder.Create(builder.Configuration);

    // Apply custom configurations
    setupPresentationLayerOptions(presentationOptions);

    // Add services with configured options
    DependencyInjection.AddPresentationLayer(builder.Services, builder.Configuration, presentationOptions);

    return builder;
  }

  /// <summary> Adds the application layer services to the WebApplicationBuilder. Includes CQRS handlers, domain services, repositories, and business logic components. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  /// <exception cref="ArgumentNullException"> Thrown when the builder is null </exception>
  public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // Get all GameGuild assemblies automatically to scan for CQRS handlers
    var assemblies = DependencyInjection.GetAssembliesByPattern();

    // Add CQRS services (handlers, behaviors, etc.)
    builder.Services.AddCQRS(assemblies);

    return builder;
  }

  public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder, Action<ApplicationLayerOptions> setupApplicationLayerOptions) {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(setupApplicationLayerOptions);

    // Create and configure options
    var applicationLayerOptions = ApplicationLayerOptionsBuilder.Create(builder.Configuration);

    // Apply custom configurations
    setupApplicationLayerOptions(applicationLayerOptions);

    // Add services with configured options
    builder.Services.AddApplicationLayer(builder.Configuration, applicationLayerOptions);

    return builder;
  }

  public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder, Action<InfrastructureLayerOptions> setupInfrastructureLayerOptions) {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(setupInfrastructureLayerOptions);

    // Create and configure options
    var infrastructureLayerOptions = InfrastructureLayerOptionsBuilder.Create(builder.Configuration);

    // Apply custom configurations
    setupInfrastructureLayerOptions(infrastructureLayerOptions);

    // Add services with configured options
    builder.Services.AddInfrastructureLayer(builder.Configuration, infrastructureLayerOptions);

    return builder;
  }

  /// <summary> Adds the infrastructure layer services to the WebApplicationBuilder. Includes repositories, external service integrations, and data access components. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <returns> The WebApplicationBuilder for method chaining </returns>
  /// <exception cref="ArgumentNullException"> Thrown when the builder is null </exception>
  public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // Add infrastructure services (repositories, external services, etc.)
    builder.Services.AddInfrastructureLayer(builder.Configuration);

    return builder;
  }

  /// <summary> Configures the WebApplicationBuilder with custom options for specific hosting scenarios. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <param name="setupPresentationLayerOptions"> Action to configure presentation options </param>
  /// <returns> The configured WebApplicationBuilder </returns>
  public static WebApplicationBuilder ConfigureWebApplicationWithOptions(this WebApplicationBuilder builder, Action<PresentationLayerOptions>? setupPresentationLayerOptions = null) {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ConfigureEnvironment();

    // Create and configure options
    var presentationOptions = CreatePresentationOptionsInternal(builder);

    // Apply custom configurations if provided
    setupPresentationLayerOptions?.Invoke(presentationOptions);

    // Add services by architectural layer with configured options
    DependencyInjection.AddPresentationLayer(builder.Services, builder.Configuration, presentationOptions);

    // Add application layer services (CQRS handlers, domain services)
    builder.AddApplicationLayer();

    // Add infrastructure layer services (repositories, external services)
    builder.AddInfrastructureLayer();

    return builder;
  }

  /// <summary> Configures authentication services with custom options. </summary>
  /// <param name="builder"> The WebApplicationBuilder instance </param>
  /// <param name="excludeAuth"> Whether to exclude authentication for testing </param>
  /// <returns> The configured WebApplicationBuilder </returns>
  public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder, bool excludeAuth = false) {
    if (!excludeAuth) {
      // Additional authentication configuration can be added here
      // This provides a hook for future authentication enhancements
    }

    return builder;
  }

  /// <summary> Adds custom middleware to the application pipeline. </summary>
  /// <param name="app"> The WebApplication instance </param>
  /// <param name="configureMiddleware"> Action to configure custom middleware </param>
  /// <returns> The WebApplication for method chaining </returns>
  public static WebApplication UseCustomMiddleware(this WebApplication app, Action<WebApplication> configureMiddleware) {
    ArgumentNullException.ThrowIfNull(configureMiddleware);

    configureMiddleware(app);

    return app;
  }

  /// <summary> Configures health checks endpoints and UI. </summary>
  /// <param name="app"> The WebApplication instance </param>
  /// <param name="healthCheckPath"> Path for health check endpoint (default: /health) </param>
  /// <returns> The WebApplication for method chaining </returns>
  public static WebApplication MapHealthCheckEndpoints(this WebApplication app, string healthCheckPath = "/health") {
    app.MapHealthChecks(healthCheckPath);

    return app;
  }

  // Private helper methods
  private static PresentationLayerOptions CreatePresentationOptionsInternal(WebApplicationBuilder builder) { return PresentationLayerOptionsBuilder.Create(builder.Configuration); }
}

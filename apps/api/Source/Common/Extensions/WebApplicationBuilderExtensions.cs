using DotNetEnv;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Common;

/// <summary>
/// Modern .NET extension methods for WebApplicationBuilder following best practices.
/// Provides fluent configuration with clean separation of concerns.
/// </summary>
public static class WebApplicationBuilderExtensions {
  /// <summary>
  /// Main entry point for configuring the GameGuild application.
  /// Combines all configuration steps in a single, fluent call.
  /// </summary>
  /// <param name="builder">The WebApplicationBuilder instance</param>
  /// <returns>The configured WebApplicationBuilder for method chaining</returns>
  public static WebApplicationBuilder ConfigureGameGuildApplication(this WebApplicationBuilder builder) =>
    builder
      .ConfigureEnvironment()
      .ConfigureServices();

  /// <summary>
  /// Configures environment variables and configuration sources.
  /// </summary>
  /// <param name="builder">The WebApplicationBuilder instance</param>
  /// <returns>The WebApplicationBuilder for method chaining</returns>
  public static WebApplicationBuilder ConfigureEnvironment(this WebApplicationBuilder builder) {
    // Load .env file for local development
    Env.Load();

    // Configure configuration sources with proper precedence
    builder.Configuration
           .SetBasePath(AppContext.BaseDirectory)
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables(); // Highest precedence

    return builder;
  }

  /// <summary>
  /// Configures all application services using clean architecture layers.
  /// </summary>
  /// <param name="builder">The WebApplicationBuilder instance</param>
  /// <returns>The WebApplicationBuilder for method chaining</returns>
  public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder) {
    builder.Services
           .AddPresentation(CreatePresentationOptionsInternal(builder))
           .AddApplication()
           .AddInfrastructure(builder.Configuration);

    return builder;
  }

  /// <summary>
  /// Builds the application and configures the complete request pipeline.
  /// </summary>
  /// <param name="builder">The configured WebApplicationBuilder</param>
  /// <returns>A fully configured WebApplication ready to run</returns>
  public static async Task<WebApplication> BuildWithPipelineAsync(this WebApplicationBuilder builder) {
    var app = builder.Build();

    await app.ConfigureApplicationAsync();

    return app.ConfigurePipeline();
  }

  /// <summary>
  /// Creates environment-specific presentation options.
  /// </summary>
  internal static DependencyInjection.PresentationOptions CreatePresentationOptionsInternal(WebApplicationBuilder builder) {
    var corsOptions = builder.Configuration
                             .GetSection(CorsOptions.SectionName)
                             .Get<CorsOptions>() ??
                      new CorsOptions();

    return new DependencyInjection.PresentationOptions {
      AllowedOrigins = corsOptions.AllowedOrigins,
      EnableSwagger = builder.Environment.IsDevelopment(),
      EnableHealthChecks = true,
      EnableResponseCompression = !builder.Environment.IsDevelopment(),
      EnableRateLimiting = !builder.Environment.IsDevelopment(),
      ApiTitle = "GameGuild API",
      ApiVersion = "v1"
    };
  }
}

/// <summary>
/// Extension methods for WebApplication to configure the request pipeline and application concerns.
/// </summary>
public static class WebApplicationExtensions {
  /// <summary>
  /// Configures application-specific concerns like database migration and seeding.
  /// </summary>
  /// <param name="app">The WebApplication instance</param>
  /// <returns>A task representing the async operation</returns>
  public static async Task<WebApplication> ConfigureApplicationAsync(this WebApplication app) {
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try { await app.EnsureDatabaseAsync(scope, logger); }
    catch (Exception ex) {
      logger.LogCritical(ex, "Application failed to start due to database initialization error");

      throw;
    }

    return app;
  }

  /// <summary>
  /// Configures the HTTP request pipeline with proper middleware ordering.
  /// </summary>
  /// <param name="app">The WebApplication instance</param>
  /// <returns>The WebApplication for method chaining</returns>
  public static WebApplication ConfigurePipeline(this WebApplication app) {
    // Exception handling (should be first)
    app.UseExceptionHandler();

    // Development-specific middleware
    if (app.Environment.IsDevelopment()) { app.ConfigureDevelopmentPipeline(); }

    // Security middleware
    app.UseHttpsRedirection();

    // CORS (must be before authentication)
    app.UseCors();

    // Request context logging
    app.UseRequestContextLogging();

    // Authentication and authorization
    app.UseAuthentication();

    // User and tenant context middleware (after authentication, before authorization)
    app.UseContextMiddleware();

    app.UseAuthorization();

    // Endpoint mapping
    app.MapEndpoints();
    app.MapGraphQL();
    app.MapControllers();

    return app;
  }

  /// <summary>
  /// Configures development-specific pipeline components.
  /// </summary>
  /// <param name="app">The WebApplication instance</param>
  /// <returns>The WebApplication for method chaining</returns>
  public static WebApplication ConfigureDevelopmentPipeline(this WebApplication app) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options => {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GameGuild CMS API v1");
        options.RoutePrefix = "swagger";
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
      }
    );

    return app;
  }

  /// <summary>
  /// Ensures database is created, migrated, and seeded.
  /// </summary>
  /// <param name="app">The WebApplication instance</param>
  /// <param name="scope">The service scope for dependency resolution</param>
  /// <param name="logger">Logger instance for tracking operations</param>
  /// <returns>A task representing the async operation</returns>
  private static async Task EnsureDatabaseAsync(this WebApplication app, IServiceScope scope, ILogger logger) {
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (context.Database.IsInMemory()) {
      logger.LogInformation("Using in-memory database, ensuring schema is created");
      await context.Database.EnsureCreatedAsync();
    }
    else {
      logger.LogInformation("Applying database migrations...");
      await context.Database.MigrateAsync();
      logger.LogInformation("Database migrations applied successfully");
    }

    // Seed initial data
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
    logger.LogInformation("Database seeding completed");
  }

  /// <summary>
  /// Runs the application with proper error handling and logging.
  /// </summary>
  /// <param name="app">The configured WebApplication</param>
  /// <returns>A task representing the application lifetime</returns>
  public static async Task RunGameGuildApiAsync(this WebApplication app) {
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    try {
      logger.LogInformation("Starting GameGuild API application...");
      await app.RunAsync();
    }
    catch (Exception ex) {
      logger.LogCritical(ex, "Application terminated unexpectedly");

      throw;
    }
    finally { logger.LogInformation("GameGuild API application stopped"); }
  }
}

/// <summary>
/// Factory class for creating pre-configured WebApplicationBuilder instances for different scenarios.
/// </summary>
public static class GameGuildApiBuilderFactory {
  /// <summary>
  /// Creates a WebApplicationBuilder configured for development with enhanced debugging and testing features.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for development</returns>
  public static WebApplicationBuilder CreateForDevelopment(string[] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Development-specific configuration
    builder.Environment.EnvironmentName = "Development";

    return builder.ConfigureGameGuildApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder configured for production with optimized performance and security.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for production</returns>
  public static WebApplicationBuilder CreateForProduction(string[] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Production-specific configuration
    builder.Environment.EnvironmentName = "Production";

    return builder.ConfigureGameGuildApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder configured for testing with in-memory dependencies.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for testing</returns>
  public static WebApplicationBuilder CreateForTesting(string[] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Testing-specific configuration
    builder.Environment.EnvironmentName = "Testing";
    Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");

    return builder.ConfigureGameGuildApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder with custom configuration action.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <param name="configureBuilder">Custom configuration action for the builder</param>
  /// <returns>A configured WebApplicationBuilder</returns>
  public static WebApplicationBuilder CreateCustom(string[] args, Action<WebApplicationBuilder> configureBuilder) {
    var builder = WebApplication.CreateBuilder(args);

    configureBuilder(builder);

    return builder.ConfigureGameGuildApplication();
  }
}

/// <summary>
/// Additional builder extensions for more granular control over configuration.
/// </summary>
public static class AdvancedWebApplicationBuilderExtensions {
  /// <summary>
  /// Configures the WebApplicationBuilder with custom options for specific hosting scenarios.
  /// </summary>
  /// <param name="builder">The WebApplicationBuilder instance</param>
  /// <param name="configureOptions">Action to configure presentation options</param>
  /// <returns>The configured WebApplicationBuilder</returns>
  public static WebApplicationBuilder ConfigureGameGuildApi(
    this WebApplicationBuilder builder,
    Action<DependencyInjection.PresentationOptions> configureOptions
  ) {
    builder.ConfigureEnvironment();

    // Create and configure presentation options using the shared helper
    var presentationOptions = WebApplicationBuilderExtensions.CreatePresentationOptionsInternal(builder);
    configureOptions(presentationOptions);

    // Add services by architectural layer
    builder.Services
           .AddPresentation(presentationOptions)
           .AddApplication()
           .AddInfrastructure(builder.Configuration);

    return builder;
  }

  /// <summary>
  /// Configures authentication services with custom options.
  /// </summary>
  /// <param name="builder">The WebApplicationBuilder instance</param>
  /// <param name="excludeAuth">Whether to exclude authentication for testing</param>
  /// <returns>The configured WebApplicationBuilder</returns>
  public static WebApplicationBuilder ConfigureAuthentication(
    this WebApplicationBuilder builder,
    bool excludeAuth = false
  ) {
    if (!excludeAuth) {
      // Additional authentication configuration can be added here
      // This provides a hook for future authentication enhancements
    }

    return builder;
  }

  /// <summary>
  /// Adds custom middleware to the application pipeline.
  /// </summary>
  /// <param name="app">The WebApplication instance</param>
  /// <param name="configureMiddleware">Action to configure custom middleware</param>
  /// <returns>The WebApplication for method chaining</returns>
  public static WebApplication UseCustomMiddleware(
    this WebApplication app,
    Action<WebApplication> configureMiddleware
  ) {
    configureMiddleware(app);

    return app;
  }

  /// <summary>
  /// Configures health checks endpoints and UI.
  /// </summary>
  /// <param name="app">The WebApplication instance</param>
  /// <param name="healthCheckPath">Path for health check endpoint (default: /health)</param>
  /// <returns>The WebApplication for method chaining</returns>
  public static WebApplication MapHealthChecks(
    this WebApplication app,
    string healthCheckPath = "/health"
  ) {
    app.MapHealthChecks(
      healthCheckPath,
      new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
        ResponseWriter = async (context, report) => {
          context.Response.ContentType = "application/json";
          var result = System.Text.Json.JsonSerializer.Serialize(
            new {
              status = report.Status.ToString(), checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString(), duration = entry.Value.Duration.ToString(), description = entry.Value.Description })
            }
          );
          await context.Response.WriteAsync(result);
        }
      }
    );

    return app;
  }
}

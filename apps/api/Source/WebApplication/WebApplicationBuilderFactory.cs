namespace GameGuild;

/// <summary>
/// Factory class for creating pre-configured WebApplicationBuilder instances for different scenarios.
/// </summary>
internal static class WebApplicationBuilderFactory {
  /// <summary>
  /// Creates a WebApplicationBuilder configured for the specified environment.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <param name="environment">The target environment</param>
  /// <returns>A pre-configured WebApplicationBuilder for the specified environment</returns>
  public static WebApplicationBuilder Create(string[ ] args, RuntimeEnvironment environment) {
    return environment switch {
      RuntimeEnvironment.Development => CreateForDevelopment(args),
      RuntimeEnvironment.Staging => CreateForStaging(args),
      RuntimeEnvironment.Production => CreateForProduction(args),
      RuntimeEnvironment.Testing => CreateForTesting(args),
      _ => throw new ArgumentException($"Unsupported environment: {environment}", nameof(environment))
    };
  }

  /// <summary>
  /// Creates a WebApplicationBuilder configured for development with enhanced debugging and testing features.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for development</returns>
  private static WebApplicationBuilder CreateForDevelopment(string[ ] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Development-specific configuration
    builder.Environment.EnvironmentName = "Development";

    return builder.ConfigureWebApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder configured for staging with production-like settings.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for staging</returns>
  private static WebApplicationBuilder CreateForStaging(string[ ] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Staging-specific configuration
    builder.Environment.EnvironmentName = "Staging";

    return builder.ConfigureWebApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder configured for production with optimized performance and security.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for production</returns>
  private static WebApplicationBuilder CreateForProduction(string[ ] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Production-specific configuration
    builder.Environment.EnvironmentName = "Production";

    return builder.ConfigureWebApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder configured for testing with in-memory dependencies.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <returns>A pre-configured WebApplicationBuilder for testing</returns>
  private static WebApplicationBuilder CreateForTesting(string[ ] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Testing-specific configuration
    builder.Environment.EnvironmentName = "Testing";
    Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");

    return builder.ConfigureWebApplication();
  }

  /// <summary>
  /// Creates a WebApplicationBuilder with custom configuration action.
  /// </summary>
  /// <param name="args">Command line arguments</param>
  /// <param name="configureBuilder">Custom configuration action for the builder</param>
  /// <returns>A configured WebApplicationBuilder</returns>
  public static WebApplicationBuilder CreateCustom(string[ ] args, Action<WebApplicationBuilder> configureBuilder) {
    ArgumentNullException.ThrowIfNull(configureBuilder);

    var builder = WebApplication.CreateBuilder(args);
    configureBuilder(builder);

    return builder.ConfigureWebApplication();
  }
}

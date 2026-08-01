namespace GameGuild.API;

/// <summary>
///     Extension methods for WebApplicationBuilder following SOLID principles.
///     Provides fluent configuration with clean separation of concerns.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     Configures environment variables and configuration sources.
    ///     Adds JSON configuration files with proper precedence and reload-on-change support.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance</param>
    /// <returns>The WebApplicationBuilder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when the builder is null</exception>
    public static WebApplicationBuilder AddAppSettings(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

        return builder;
    }

    /// <summary>
    ///     Adds environment variables to the configuration pipeline.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance</param>
    /// <returns>The WebApplicationBuilder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when the builder is null</exception>
    public static WebApplicationBuilder AddEnvironmentVariables(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
}

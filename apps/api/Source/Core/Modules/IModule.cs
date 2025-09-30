namespace GameGuild.Core.Modules;

/// <summary>
/// Interface for defining modules in the GameGuild application.
/// Provides a standardized approach for module registration and endpoint mapping.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Configures services for the module in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Maps endpoints for the module in the application pipeline.
    /// </summary>
    /// <param name="app">The web application to map endpoints to</param>
    /// <returns>The web application for chaining</returns>
    WebApplication MapEndpoints(WebApplication app);

    /// <summary>
    /// Gets the name of the module for identification and logging.
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Gets the version of the module for tracking and compatibility.
    /// </summary>
    string ModuleVersion { get; }
}

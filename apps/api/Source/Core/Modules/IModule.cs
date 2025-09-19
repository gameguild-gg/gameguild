namespace GameGuild.Core.Modules;

/// <summary>
/// Interface for defining modules in the GameGuild application.
/// Provides a standardized approach for module registration and endpoint mapping.
/// </summary>
public interface IModule {
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

/// <summary>
/// Base abstract class for modules providing common functionality.
/// Implements standard module patterns and logging.
/// </summary>
public abstract class ModuleBase : IModule {
    private readonly ILogger<ModuleBase> _logger;

    protected ModuleBase() {
        // Create a logger using the factory method
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ModuleBase>();
    }

    /// <summary>
    /// Configures services for the module. Override to add module-specific services.
    /// </summary>
    public virtual IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        _logger.LogInformation("Configuring services for {ModuleName} v{ModuleVersion}", ModuleName, ModuleVersion);
        return services;
    }

    /// <summary>
    /// Maps endpoints for the module. Override to add module-specific endpoints.
    /// </summary>
    public virtual WebApplication MapEndpoints(WebApplication app) {
        _logger.LogInformation("Mapping endpoints for {ModuleName} v{ModuleVersion}", ModuleName, ModuleVersion);
        return app;
    }

    /// <summary>
    /// Gets the name of the module. Must be implemented by derived classes.
    /// </summary>
    public abstract string ModuleName { get; }

    /// <summary>
    /// Gets the version of the module. Defaults to "1.0.0" but can be overridden.
    /// </summary>
    public virtual string ModuleVersion => "1.0.0";
}

/// <summary>
/// Extension methods for registering modules in the application.
/// </summary>
public static class ModuleExtensions {
    /// <summary>
    /// Registers a module in the service collection.
    /// </summary>
    /// <typeparam name="TModule">The type of module to register</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services, IConfiguration configuration)
        where TModule : class, IModule, new() {
        var module = new TModule();
        return module.ConfigureServices(services, configuration);
    }

    /// <summary>
    /// Maps endpoints for a module in the application.
    /// </summary>
    /// <typeparam name="TModule">The type of module to map endpoints for</typeparam>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseModule<TModule>(this WebApplication app)
        where TModule : class, IModule, new() {
        var module = new TModule();
        return module.MapEndpoints(app);
    }

    /// <summary>
    /// Registers multiple modules in the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="modules">The modules to register</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration, params IModule[] modules) {
        foreach (var module in modules) {
            module.ConfigureServices(services, configuration);
        }
        return services;
    }

    /// <summary>
    /// Maps endpoints for multiple modules in the application.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="modules">The modules to map endpoints for</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseModules(this WebApplication app, params IModule[] modules) {
        foreach (var module in modules) {
            module.MapEndpoints(app);
        }
        return app;
    }
}

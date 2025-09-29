namespace GameGuild.Core.Modules;

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
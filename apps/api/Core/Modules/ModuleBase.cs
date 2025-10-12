namespace GameGuild.Core.Modules;

/// <summary>
/// Base abstract class for modules providing common functionality.
/// Implements standard module patterns and logging.
/// </summary>
public abstract class ModuleBase : IModule
{
    private readonly ILogger<ModuleBase> _logger;

    protected ModuleBase()
    {
        // Create a logger using the factory method
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ModuleBase>();
    }

    /// <summary>
    /// Configures services for the module. Override to add module-specific services.
    /// </summary>
    public virtual IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        _logger.LogInformation("Configuring services for {ModuleName} v{ModuleVersion}", ModuleName, ModuleVersion);

        return services;
    }

    /// <summary>
    /// Maps endpoints for the module. Override to add module-specific endpoints.
    /// </summary>
    public virtual WebApplication MapEndpoints(WebApplication app)
    {
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

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild;

/// <summary>
///     Standard interface for GameGuild modules.
///     Modules are self-contained vertical slices that encapsulate domain logic,
///     data access, and presentation concerns.
/// </summary>
public interface IModule
{
    /// <summary>
    ///     Gets the unique name of the module.
    ///     Used for logging, configuration sections, and module discovery.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the order in which this module should be registered.
    ///     Lower values are registered first. Default is 100.
    ///     Core modules (SharedKernel, Authentication) should use lower values.
    /// </summary>
    int Order => 100;

    /// <summary>
    ///     Gets whether this module is enabled by default.
    ///     Can be overridden via configuration: Modules:{ModuleName}:Enabled
    /// </summary>
    bool EnabledByDefault => true;

    /// <summary>
    ///     Gets the modules that this module depends on.
    ///     The module system will ensure dependencies are registered first.
    /// </summary>
    IReadOnlyList<Type> Dependencies => [];

    /// <summary>
    ///     Registers services for this module into the dependency injection container.
    ///     Called during application startup in the order determined by <see cref="Order"/> and <see cref="Dependencies"/>.
    /// </summary>
    /// <param name="services">The service collection to register services into</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    ///     Maps endpoints for this module.
    ///     Called after all modules have registered their services.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}

/// <summary>
///     Base implementation of <see cref="IModule"/> with sensible defaults.
///     Modules can inherit from this class instead of implementing the interface directly.
/// </summary>
public abstract class ModuleBase : IModule
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual int Order => 100;

    /// <inheritdoc />
    public virtual bool EnabledByDefault => true;

    /// <inheritdoc />
    public virtual IReadOnlyList<Type> Dependencies => [];

    /// <inheritdoc />
    public abstract IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <inheritdoc />
    public virtual IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}

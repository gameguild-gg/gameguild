using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild;

/// <summary>
///     Extension methods for registering <see cref="IModule"/> implementations.
///     Use these to register individual modules by type or to discover and register all modules
///     from assemblies automatically.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    ///     Registers a single module's services using the <see cref="IModule"/> pattern.
    /// </summary>
    /// <typeparam name="TModule">The module type implementing <see cref="IModule"/></typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddModule<TModule>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TModule : class, IModule, new()
    {
        var module = new TModule();
        return module.ConfigureServices(services, configuration);
    }

    /// <summary>
    ///     Maps a single module's endpoints using the <see cref="IModule"/> pattern.
    /// </summary>
    /// <typeparam name="TModule">The module type implementing <see cref="IModule"/></typeparam>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder UseModule<TModule>(
        this IEndpointRouteBuilder endpoints)
        where TModule : class, IModule, new()
    {
        var module = new TModule();
        return module.MapEndpoints(endpoints);
    }
}

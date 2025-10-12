using GameGuild.Modules.Permissions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the Permissions module services.
/// </summary>
public static class PermissionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Permissions module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration to use for module setup.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPermissionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var module = new PermissionsModule();
        return module.ConfigureServices(services, configuration);
    }
}
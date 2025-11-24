using GameGuild.CQRS;
using GameGuild.Resources.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Resources.Extensions;

/// <summary>
///     Extension methods for registering Resources module services
/// </summary>
public static class ResourcesServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the resource quota pipeline behavior to the service collection.
    ///     This behavior will automatically validate and enforce quotas for commands
    ///     decorated with the [RequiresQuota] attribute.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddResourceQuotaBehavior(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResourceQuotaBehavior<,>));
        return services;
    }
}

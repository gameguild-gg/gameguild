using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Resources;

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

    /// <summary>
    ///     Registers a custom resource usage type for quota tracking.
    ///     Call this during application startup before services are built.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="info">The resource type information to register</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// services.RegisterResourceUsageType(new ResourceUsageTypeInfo
    /// {
    ///     Id = 1001,
    ///     Key = "Assets",
    ///     DisplayName = "Assets",
    ///     Description = "File assets stored per tenant",
    ///     Unit = "count",
    ///     OwnerModule = "GameGuild.Assets"
    /// });
    /// </example>
    public static IServiceCollection RegisterResourceUsageType(
        this IServiceCollection services,
        ResourceUsageTypeInfo info)
    {
        ResourceUsageTypeRegistry.Register(info);
        return services;
    }

    /// <summary>
    ///     Registers multiple custom resource usage types for quota tracking.
    /// </summary>
    public static IServiceCollection RegisterResourceUsageTypes(
        this IServiceCollection services,
        params ResourceUsageTypeInfo[] types)
    {
        foreach (var info in types)
        {
            ResourceUsageTypeRegistry.Register(info);
        }
        return services;
    }

    /// <summary>
    ///     Seals the resource usage type registry, preventing further registrations.
    ///     Call this after all modules have registered their types.
    /// </summary>
    public static IServiceCollection SealResourceUsageTypeRegistry(this IServiceCollection services)
    {
        ResourceUsageTypeRegistry.Seal();
        return services;
    }
}

using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Tenants.Extensions;

/// <summary>
///     Dependency injection extensions for the Tenants module
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Tenants module services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddTenantsModule(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}

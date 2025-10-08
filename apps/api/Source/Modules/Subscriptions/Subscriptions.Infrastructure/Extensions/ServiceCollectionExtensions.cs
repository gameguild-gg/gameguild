using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Repositories;

namespace GameGuild.Modules.Subscriptions.Extensions;

/// <summary>
///     Service collection extensions for the Subscriptions Infrastructure layer
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Subscriptions Infrastructure services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSubscriptionsInfrastructure<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Add repositories with concrete DbContext
        services.AddScoped<ISubscriptionRepository>(provider =>
            new SubscriptionRepository(provider.GetRequiredService<TDbContext>()));

        return services;
    }
}


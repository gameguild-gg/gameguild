using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Infrastructure;

namespace GameGuild.Modules.Subscriptions;

/// <summary>
/// Extension methods for registering Subscriptions module services
/// </summary>
public static class SubscriptionsModule
{
    /// <summary>
    /// Registers all Subscriptions module services and repositories
    /// </summary>
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services)
    {
        // Register subscription repository
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        // Domain event handlers and command handlers are automatically registered by MediatR
        // via the assembly scanning in AddOptimizedHandlers

        return services;
    }
}
using GameGuild.Billing.Abstractions;
using GameGuild.Billing.Configuration;
using GameGuild.Billing.Repositories;
using GameGuild.Billing.Services;
using GameGuild.CQRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Billing.Extensions;

/// <summary>
///     Extension methods for registering Billing module services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Add Billing module services to the service collection
    /// </summary>
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register CQRS handlers from this assembly
        services.AddCqrs(typeof(ServiceCollectionExtensions).Assembly);

        // Register configuration
        services.Configure<BillingConfiguration>(configuration.GetSection(BillingConfiguration.SectionName));

        // Register repositories
        services.AddScoped<IBillingWebhookRepository, BillingWebhookRepository>();

        // Register services
        services.AddScoped<IBillingWebhookService, BillingWebhookService>();

        return services;
    }

    /// <summary>
    ///     Add Billing webhook processing services
    /// </summary>
    public static IServiceCollection AddBillingWebhooks(this IServiceCollection services)
    {
        // Register webhook-specific services
        services.AddScoped<IBillingWebhookRepository, BillingWebhookRepository>();
        services.AddScoped<IBillingWebhookService, BillingWebhookService>();

        return services;
    }
}

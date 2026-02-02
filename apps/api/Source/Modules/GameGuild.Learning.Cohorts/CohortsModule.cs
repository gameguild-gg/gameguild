using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Cohorts;

/// <summary>
/// Module registration for the Cohorts/Scheduling system.
/// </summary>
public static class CohortsModule
{
    /// <summary>
    /// Adds cohort services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddCohortsModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICohortService, CohortService>();

        return services;
    }

    /// <summary>
    /// Maps cohort endpoints if using minimal APIs.
    /// </summary>
    public static IEndpointRouteBuilder MapCohortsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Controllers are auto-discovered, but this can be used for minimal API routes
        return endpoints;
    }
}

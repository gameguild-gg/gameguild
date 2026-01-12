using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Extension methods for integrating the Actor context with the Authorization module.
/// </summary>
public static class ActorContextExtensions
{
    /// <summary>
    ///     Registers the Actor context infrastructure and adapters for backward compatibility.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="useActorBasedContexts">
    ///     If true, replaces the HTTP-based IUserContext/ITenantContext/IPermissionsContext
    ///     with actor-based adapters. Recommended for new projects or gradual migration.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActorContextIntegration(
        this IServiceCollection services,
        bool useActorBasedContexts = false)
    {
        // Register core Actor context accessor (singleton using AsyncLocal)
        services.AddSingleton<IActorContextAccessor, ActorContextAccessor>();

        if (useActorBasedContexts)
        {
            // Replace HTTP-based contexts with Actor-based adapters
            // These are marked [Obsolete] to encourage migration to IActorContextAccessor
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddScoped<IUserContext, ActorBasedUserContext>();
            services.AddScoped<ITenantContext, ActorBasedTenantContext>();
            services.AddScoped<IPermissionsContext, ActorBasedPermissionsContext>();
#pragma warning restore CS0618
        }

        return services;
    }

    /// <summary>
    ///     Adds the ActorContext middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This middleware should be added after authentication middleware and before
    ///         authorization middleware. It builds the ActorContext from claims and tenant resolution.
    ///     </para>
    ///     <code>
    ///     app.UseAuthentication();
    ///     app.UseActorContext();  // &lt;-- Add here
    ///     app.UseAuthorization();
    ///     </code>
    /// </remarks>
    public static IApplicationBuilder UseActorContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ActorContextMiddleware>();
    }
}

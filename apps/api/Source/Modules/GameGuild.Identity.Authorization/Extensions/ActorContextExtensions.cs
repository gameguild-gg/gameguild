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
    ///     Registers the Actor context infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActorContextIntegration(this IServiceCollection services)
    {
        // Register core Actor context accessor (singleton using AsyncLocal)
        services.AddSingleton<IActorContextAccessor, ActorContextAccessor>();

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

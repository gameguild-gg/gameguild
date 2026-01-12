using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Context.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Identity.Context;

/// <summary>
///     Identity Context module that aggregates all identity-related modules.
///     This module provides a unified entry point for configuring authentication,
///     authorization, users, and tenant management.
/// </summary>
public static class IdentityContextModule
{
    /// <summary>
    ///     Register all identity context services including the actor context accessor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentityContextModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register the Actor context accessor (singleton using AsyncLocal)
        services.AddSingleton<IActorContextAccessor, ActorContextAccessor>();

        // Register the legacy Identity Context
        services.AddScoped<IIdentityContext, IdentityContext>();
        
        return services;
    }

    /// <summary>
    ///     Configure the identity context module in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         IMPORTANT: After configuring all security middleware (Authentication, Tenant,
    ///         ActorContext, Authorization), call <see cref="MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder"/>
    ///         to ensure they are registered in the correct order.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         app.UseAuthentication();
    ///         app.UseTenantMiddleware();
    ///         app.UseActorContext();
    ///         app.UseAuthorization();
    ///         
    ///         // Validate middleware order (throws if incorrect)
    ///         app.ValidateSecurityMiddlewareOrder();
    ///         </code>
    ///     </example>
    /// </remarks>
    public static IApplicationBuilder UseIdentityContextModule(
        this IApplicationBuilder app)
    {
        // Note: ActorContext middleware should be added by the Authorization module
        // after authentication and before authorization
        return app;
    }
}

using System.Globalization;

using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Middleware that populates context information early in the request pipeline.
///     Migrated to use ActorContext via IActorContextAccessor.
/// </summary>
public class ContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, IActorContextAccessor actorContextAccessor, ILocalizationContext localizationContext)
    {
        // ActorContext is now the primary context - resolved from IActorContextAccessor
        var actor = actorContextAccessor.ActorContext;

        // Store ActorContext in HttpContext.Items for easy access by legacy code
        httpContext.Items[HttpContextKeys.ActorContext] = actor;
        httpContext.Items[HttpContextKeys.LocalizationContext] = localizationContext;

        // Set culture and UI culture for the request (set both thread and CultureInfo static props to be robust in async test environments)
        var cultureCode = localizationContext.CultureCode ?? "en-US";
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        await next(httpContext).ConfigureAwait(false);
    }
}

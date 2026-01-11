using System.Globalization;

using Microsoft.AspNetCore.Http;

namespace GameGuild.Authorization;

/// <summary>
///     Middleware that populates all context interfaces early in the request pipeline
///     This ensures contexts are available for downstream middleware and filters
/// </summary>
public class ContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, IUserContext userContext, ITenantContext tenantContext, IPermissionsContext permissionsContext, ILocalizationContext localizationContext)
    {
        // Contexts are resolved from DI as scoped services
        // They will extract data from HttpContext when their properties are accessed
        // This middleware just ensures they're resolved early in the pipeline

        // Store contexts in HttpContext.Items for easy access
        httpContext.Items["UserContext"] = userContext;
        httpContext.Items["TenantContext"] = tenantContext;
        httpContext.Items["PermissionsContext"] = permissionsContext;
        httpContext.Items["LocalizationContext"] = localizationContext;

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

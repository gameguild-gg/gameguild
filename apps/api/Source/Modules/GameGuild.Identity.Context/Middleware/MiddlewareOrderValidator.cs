using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Context.Middleware;

/// <summary>
///     Validates that security-critical middleware is registered in the correct order.
///     REQUIRED ORDER:
///     1. Authentication (validates JWT, populates ClaimsPrincipal)
///     2. TenantMiddleware (resolves tenant ID, stores in HttpContext)
///     3. ActorContextMiddleware (builds ActorContext from claims + tenant)
///     4. Authorization (evaluates policies using ActorContext)
/// </summary>
/// <remarks>
///     <para>
///         Incorrect middleware order can cause security vulnerabilities:
///         - ActorContext before Tenant → ActorContext built with null TenantId
///         - Tenant before Authentication → Cannot determine user for tenant membership validation
///         - Authorization before ActorContext → Policy evaluation uses incomplete context
///     </para>
/// </remarks>
public static class MiddlewareOrderValidator
{
    /// <summary>
    ///     Validates that security middleware is registered in the correct order.
    ///     Call this method after all middleware has been registered to ensure
    ///     the pipeline is configured correctly.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when middleware is missing or registered in the wrong order.
    /// </exception>
    public static void ValidateSecurityMiddlewareOrder(this IApplicationBuilder app)
    {
        var pipeline = GetMiddlewarePipeline(app);

        // Find indices of security middleware
        var authenticationIndex = FindMiddlewareIndex(pipeline, "AuthenticationMiddleware");
        var tenantIndex = FindMiddlewareIndex(pipeline, "TenantMiddleware");
        var actorContextIndex = FindMiddlewareIndex(pipeline, "ActorContextMiddleware");
        var authorizationIndex = FindMiddlewareIndex(pipeline, "AuthorizationMiddleware");

        // Check for missing middleware (optional - only validate if registered)
        var hasAuthentication = authenticationIndex >= 0;
        var hasTenant = tenantIndex >= 0;
        var hasActorContext = actorContextIndex >= 0;
        var hasAuthorization = authorizationIndex >= 0;

        // If ActorContext is registered, validate the full chain
        if (hasActorContext)
        {
            if (!hasAuthentication)
            {
                throw new InvalidOperationException(
                    "ActorContextMiddleware requires Authentication middleware. " +
                    "Call app.UseAuthentication() before app.UseActorContext().");
            }

            if (!hasTenant)
            {
                throw new InvalidOperationException(
                    "ActorContextMiddleware requires TenantMiddleware. " +
                    "Call app.UseTenantMiddleware() before app.UseActorContext().");
            }

            // Validate order: Authentication → Tenant → ActorContext → Authorization
            if (tenantIndex < authenticationIndex)
            {
                throw new InvalidOperationException(
                    "TenantMiddleware must run AFTER Authentication. " +
                    $"Current order: TenantMiddleware (position {tenantIndex}) comes before " +
                    $"AuthenticationMiddleware (position {authenticationIndex}). " +
                    "Fix: Move app.UseTenantMiddleware() to after app.UseAuthentication().");
            }

            if (actorContextIndex < tenantIndex)
            {
                throw new InvalidOperationException(
                    "ActorContextMiddleware must run AFTER TenantMiddleware. " +
                    $"Current order: ActorContextMiddleware (position {actorContextIndex}) comes before " +
                    $"TenantMiddleware (position {tenantIndex}). " +
                    "Fix: Move app.UseActorContext() to after app.UseTenantMiddleware().");
            }

            if (hasAuthorization && authorizationIndex < actorContextIndex)
            {
                throw new InvalidOperationException(
                    "Authorization must run AFTER ActorContextMiddleware. " +
                    $"Current order: AuthorizationMiddleware (position {authorizationIndex}) comes before " +
                    $"ActorContextMiddleware (position {actorContextIndex}). " +
                    "Fix: Move app.UseAuthorization() to after app.UseActorContext().");
            }
        }

        // If only using legacy contexts, validate basic order
        if (hasAuthentication && hasTenant && !hasActorContext)
        {
            if (tenantIndex < authenticationIndex)
            {
                throw new InvalidOperationException(
                    "TenantMiddleware must run AFTER Authentication. " +
                    $"Current order: TenantMiddleware (position {tenantIndex}) comes before " +
                    $"AuthenticationMiddleware (position {authenticationIndex}).");
            }
        }
    }

    private static List<string> GetMiddlewarePipeline(IApplicationBuilder app)
    {
        // Use reflection to get the middleware pipeline
        var applicationBuilder = app as ApplicationBuilder;
        if (applicationBuilder == null)
        {
            throw new InvalidOperationException(
                "Unable to validate middleware order: IApplicationBuilder is not ApplicationBuilder");
        }

        var middlewareField = typeof(ApplicationBuilder)
            .GetField("_components", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var components = middlewareField.GetValue(applicationBuilder) as IList<Func<RequestDelegate, RequestDelegate>>
            ?? new List<Func<RequestDelegate, RequestDelegate>>();

        // Extract middleware type names from components
        var middlewareTypes = new List<string>();
        foreach (var component in components)
        {
            var target = component.Target;
            var componentTypeName = target?.GetType().Name ?? "Unknown";
            middlewareTypes.Add(componentTypeName);

            // Components registered through UseMiddleware<T>() arrive as compiler-generated
            // closures whose target type name is opaque (e.g. <>c__DisplayClass...). The
            // middleware identity is recoverable from the closure's captured state: the
            // middleware System.Type (and for interface middleware, sometimes an instance).
            // Without this, a real pipeline reports no middleware at all and validation
            // silently degrades to a no-op (issue #1248 N3).
            if (target is not null && componentTypeName.StartsWith("<", StringComparison.Ordinal))
            {
                middlewareTypes.AddRange(ExtractClosureMiddlewareNames(target));
            }
        }

        return middlewareTypes;
    }

    /// <summary>
    ///     Collects middleware-looking names captured by a compiler-generated UseMiddleware
    ///     closure: captured <see cref="Type"/> values and captured object instances whose
    ///     type names end with "Middleware".
    /// </summary>
    private static IEnumerable<string> ExtractClosureMiddlewareNames(object closure)
    {
        const string Suffix = "Middleware";
        var fields = closure.GetType().GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            var value = field.GetValue(closure);
            switch (value)
            {
                case Type capturedType when capturedType.Name.EndsWith(Suffix, StringComparison.Ordinal):
                    yield return capturedType.Name;
                    break;
                case { } captured when captured.GetType().Name.EndsWith(Suffix, StringComparison.Ordinal)
                                      && captured is not Type:
                    yield return captured.GetType().Name;
                    break;
            }
        }
    }

    private static int FindMiddlewareIndex(List<string> pipeline, string middlewareName)
    {
        for (int i = 0; i < pipeline.Count; i++)
        {
            var name = pipeline[i];
            if (name.Contains(middlewareName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GameGuild;

/// <summary>
///     Middleware that adds security headers to all responses.
///     Implements best practices for API security hardening.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions? options = null)
{
    private readonly SecurityHeadersOptions _options = options ?? new SecurityHeadersOptions();

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before the response starts
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // X-Content-Type-Options: Prevents MIME-type sniffing
            if (_options.EnableXContentTypeOptions)
            {
                headers.TryAdd("X-Content-Type-Options", "nosniff");
            }

            // X-Frame-Options: Prevents clickjacking (legacy, use CSP frame-ancestors for modern browsers)
            if (_options.EnableXFrameOptions)
            {
                headers.TryAdd("X-Frame-Options", _options.XFrameOptionsValue);
            }

            // Referrer-Policy: Controls how much referrer info is sent
            if (_options.EnableReferrerPolicy)
            {
                headers.TryAdd("Referrer-Policy", _options.ReferrerPolicyValue);
            }

            // X-XSS-Protection: Legacy XSS filter (disabled in modern browsers, but still set for older ones)
            if (_options.EnableXXssProtection)
            {
                headers.TryAdd("X-XSS-Protection", "0");
            }

            // Content-Security-Policy: Controls resource loading
            // Note: For APIs, a restrictive policy is fine; for Swagger UI, needs exceptions
            if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicyValue))
            {
                // Check if this is a Swagger/documentation path - use relaxed policy
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                if (path.StartsWith("/documentation") || path.StartsWith("/swagger"))
                {
                    headers.TryAdd("Content-Security-Policy", _options.SwaggerContentSecurityPolicyValue);
                }
                else
                {
                    headers.TryAdd("Content-Security-Policy", _options.ContentSecurityPolicyValue);
                }
            }

            // Permissions-Policy: Controls browser features (formerly Feature-Policy)
            if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicyValue))
            {
                headers.TryAdd("Permissions-Policy", _options.PermissionsPolicyValue);
            }

            // Cache-Control for sensitive endpoints (can be overridden by specific endpoints)
            if (_options.EnableNoCacheForSensitiveEndpoints)
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                if (IsSensitivePath(path))
                {
                    headers.TryAdd("Cache-Control", "no-store, no-cache, must-revalidate");
                    headers.TryAdd("Pragma", "no-cache");
                }
            }

            return Task.CompletedTask;
        });

        await next(context).ConfigureAwait(false);
    }

    private static bool IsSensitivePath(string path)
    {
        // Paths that should never be cached — use segment-aware matching
        return path.StartsWith("/auth", StringComparison.Ordinal) ||
               path.StartsWith("/login", StringComparison.Ordinal) ||
               path.StartsWith("/token", StringComparison.Ordinal) ||
               path.Contains("/auth/", StringComparison.Ordinal) ||
               path.Contains("/login/", StringComparison.Ordinal) ||
               path.Contains("/token/", StringComparison.Ordinal) ||
               path.Contains("/password/", StringComparison.Ordinal) ||
               path.EndsWith("/password", StringComparison.Ordinal);
    }
}

/// <summary>
///     Configuration options for SecurityHeadersMiddleware.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    ///     Enables X-Content-Type-Options: nosniff header.
    ///     Prevents MIME-type sniffing attacks.
    /// </summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>
    ///     Enables X-Frame-Options header to prevent clickjacking.
    /// </summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>
    ///     Value for X-Frame-Options header. Default is "DENY".
    ///     Options: "DENY", "SAMEORIGIN"
    /// </summary>
    public string XFrameOptionsValue { get; set; } = "DENY";

    /// <summary>
    ///     Enables Referrer-Policy header.
    /// </summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    ///     Value for Referrer-Policy header. Default is "strict-origin-when-cross-origin".
    ///     Options: "no-referrer", "no-referrer-when-downgrade", "origin", 
    ///     "origin-when-cross-origin", "same-origin", "strict-origin", 
    ///     "strict-origin-when-cross-origin", "unsafe-url"
    /// </summary>
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    ///     Enables X-XSS-Protection header. Set to "0" to disable as recommended by OWASP.
    ///     Modern browsers have removed XSS auditor, setting to 0 prevents edge cases.
    /// </summary>
    public bool EnableXXssProtection { get; set; } = true;

    /// <summary>
    ///     Enables Content-Security-Policy header.
    /// </summary>
    public bool EnableContentSecurityPolicy { get; set; } = true;

    /// <summary>
    ///     Value for Content-Security-Policy header for API endpoints.
    ///     Default is a restrictive policy suitable for JSON APIs.
    /// </summary>
    public string ContentSecurityPolicyValue { get; set; } = "default-src 'none'; frame-ancestors 'none'";

    /// <summary>
    ///     Value for Content-Security-Policy header for Swagger UI.
    ///     More permissive to allow Swagger assets to load.
    /// </summary>
    public string SwaggerContentSecurityPolicyValue { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'";

    /// <summary>
    ///     Enables Permissions-Policy header (formerly Feature-Policy).
    /// </summary>
    public bool EnablePermissionsPolicy { get; set; } = true;

    /// <summary>
    ///     Value for Permissions-Policy header.
    ///     Restricts browser features that APIs typically don't need.
    /// </summary>
    public string PermissionsPolicyValue { get; set; } = 
        "accelerometer=(), " +
        "camera=(), " +
        "geolocation=(), " +
        "gyroscope=(), " +
        "magnetometer=(), " +
        "microphone=(), " +
        "payment=(), " +
        "usb=()";

    /// <summary>
    ///     Enables no-cache headers for sensitive endpoints (auth, user, etc.)
    /// </summary>
    public bool EnableNoCacheForSensitiveEndpoints { get; set; } = true;
}

/// <summary>
///     Extension methods for adding SecurityHeaders middleware to the application pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    ///     Adds the security headers middleware to the application pipeline.
    ///     Should be placed early, after HTTPS redirection but before routing.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="options">Optional configuration for security headers</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, SecurityHeadersOptions? options = null)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(options ?? new SecurityHeadersOptions());
    }

    /// <summary>
    ///     Adds the security headers middleware with custom configuration.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configure">Action to configure security header options</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Action<SecurityHeadersOptions> configure)
    {
        var options = new SecurityHeadersOptions();
        configure(options);
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}

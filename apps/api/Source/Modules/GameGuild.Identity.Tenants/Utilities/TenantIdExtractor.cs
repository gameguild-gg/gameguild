using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Tenants.Utilities;

/// <summary>
///     Utility for extracting tenant ID from HTTP request sources.
///     Centralizes the common patterns for tenant ID extraction across the application.
/// </summary>
/// <remarks>
///     <para>
///         This utility provides low-level extraction methods for tenant IDs from various
///         HTTP request sources (headers, query strings, route values, domains).
///     </para>
///     <para>
///         For full tenant resolution with entity lookups and validation, use TenantMiddleware
///         or inject ICurrentTenant. This utility is for simple extraction scenarios.
///     </para>
/// </remarks>
public static class TenantIdExtractor
{
    /// <summary>
    ///     Default header name for tenant ID.
    /// </summary>
    public const string DefaultTenantIdHeader = "X-Tenant-Id";

    /// <summary>
    ///     Default query/route parameter name for tenant ID.
    /// </summary>
    public const string DefaultTenantIdKey = "tenantId";

    /// <summary>
    ///     Tries to extract tenant ID from the X-Tenant-Id header.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="headerName">Optional custom header name (defaults to "X-Tenant-Id")</param>
    /// <returns>Tenant ID if found and valid Guid, otherwise null</returns>
    public static Guid? FromHeader(HttpContext context, string headerName = DefaultTenantIdHeader)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var tenantIdHeader)
            && Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    /// <summary>
    ///     Tries to extract tenant ID from the query string.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="queryKey">Optional custom query key (defaults to "tenantId")</param>
    /// <returns>Tenant ID if found and valid Guid, otherwise null</returns>
    public static Guid? FromQuery(HttpContext context, string queryKey = DefaultTenantIdKey)
    {
        if (context.Request.Query.TryGetValue(queryKey, out var tenantIdQuery)
            && Guid.TryParse(tenantIdQuery, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    /// <summary>
    ///     Tries to extract tenant ID from route values.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="routeKey">Optional custom route key (defaults to "tenantId")</param>
    /// <returns>Tenant ID if found and valid Guid, otherwise null</returns>
    public static Guid? FromRoute(HttpContext context, string routeKey = DefaultTenantIdKey)
    {
        if (context.Request.RouteValues.TryGetValue(routeKey, out var tenantIdRoute)
            && Guid.TryParse(tenantIdRoute?.ToString(), out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    /// <summary>
    ///     Tries to extract tenant ID from any source in priority order:
    ///     1. Header (X-Tenant-Id)
    ///     2. Query string (tenantId)
    ///     3. Route values (tenantId)
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="headerName">Optional custom header name</param>
    /// <param name="paramKey">Optional custom query/route key</param>
    /// <returns>Tenant ID from the first available source, or null if not found</returns>
    public static Guid? FromAnySource(
        HttpContext context,
        string? headerName = null,
        string? paramKey = null)
    {
        var header = headerName ?? DefaultTenantIdHeader;
        var key = paramKey ?? DefaultTenantIdKey;

        return FromHeader(context, header)
            ?? FromQuery(context, key)
            ?? FromRoute(context, key);
    }

    /// <summary>
    ///     Extracts the host/domain from the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The host string</returns>
    public static string GetHost(HttpContext context)
    {
        return context.Request.Host.Host;
    }

    /// <summary>
    ///     Checks if the current request is from localhost.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>True if localhost, false otherwise</returns>
    public static bool IsLocalhost(HttpContext context)
    {
        var host = context.Request.Host.Host;
        return IsLocalhost(host);
    }

    /// <summary>
    ///     Checks if a host string is localhost.
    /// </summary>
    /// <param name="host">The host string</param>
    /// <returns>True if localhost, false otherwise</returns>
    public static bool IsLocalhost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Extracts subdomain from a host string.
    ///     For example, "tenant.example.com" returns "tenant".
    /// </summary>
    /// <param name="host">The host string</param>
    /// <returns>Subdomain, or null if not found</returns>
    public static string? ExtractSubdomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            // Return first part as subdomain (e.g., "tenant" from "tenant.example.com")
            return parts[0];
        }

        return null;
    }

    /// <summary>
    ///     Extracts subdomain from the HTTP request host.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>Subdomain, or null if not found</returns>
    public static string? ExtractSubdomain(HttpContext context)
    {
        return ExtractSubdomain(GetHost(context));
    }
}

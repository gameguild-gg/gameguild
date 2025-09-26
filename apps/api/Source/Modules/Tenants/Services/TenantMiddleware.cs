namespace GameGuild.Modules.Tenants;

/// <summary>
/// Middleware that resolves the current tenant for each request
/// Uses cached tenant data and defaults to the default tenant if none is specified
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ITenantContext tenantContext)
    {
        try
        {
            var tenant = await ResolveTenantAsync(context, tenantService);
            tenantContext.SetCurrentTenant(tenant);

            _logger.LogDebug("Resolved tenant: {TenantId} - {TenantSlug}", tenant?.Id, tenant?.Slug ?? "default");

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for request");

            // Try to set default tenant as fallback
            try
            {
                var defaultTenant = await tenantService.GetDefaultTenantAsync();
                tenantContext.SetCurrentTenant(defaultTenant);
                _logger.LogWarning("Fallback to default tenant: {TenantId}", defaultTenant?.Id);

                await _next(context);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Error setting fallback tenant");

                throw;
            }
        }
    }

    private async Task<Tenant?> ResolveTenantAsync(HttpContext context, ITenantService tenantService)
    {
        // 1. Try to resolve tenant from subdomain
        var tenant = await ResolveTenantFromSubdomain(context, tenantService);

        if (tenant != null)
        {
            _logger.LogDebug("Resolved tenant from subdomain: {TenantSlug}", tenant.Slug);

            return tenant;
        }

        // 2. Try to resolve tenant from custom header
        tenant = await ResolveTenantFromHeader(context, tenantService);

        if (tenant != null)
        {
            _logger.LogDebug("Resolved tenant from header: {TenantSlug}", tenant.Slug);

            return tenant;
        }

        // 3. Try to resolve tenant from route parameter
        tenant = await ResolveTenantFromRoute(context, tenantService);

        if (tenant != null)
        {
            _logger.LogDebug("Resolved tenant from route: {TenantSlug}", tenant.Slug);

            return tenant;
        }

        // 4. Fall back to default tenant
        tenant = await tenantService.GetDefaultTenantAsync();

        if (tenant != null) { _logger.LogDebug("Using default tenant: {TenantSlug}", tenant.Slug); }
        else { _logger.LogWarning("No default tenant found"); }

        return tenant;
    }

    private async Task<Tenant?> ResolveTenantFromSubdomain(HttpContext context, ITenantService tenantService)
    {
        var host = context.Request.Host.Host;

        if (string.IsNullOrEmpty(host)) return null;

        // Check if it's a subdomain (contains dots and isn't localhost or IP)
        var parts = host.Split('.');

        if (parts.Length < 2 || host == "localhost" || IsIpAddress(host)) return null;

        var subdomain = parts[0];

        return await tenantService.GetTenantBySlugAsync(subdomain);
    }

    private async Task<Tenant?> ResolveTenantFromHeader(HttpContext context, ITenantService tenantService)
    {
        // Check for X-Tenant-Slug header
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var tenantSlugHeader))
        {
            var slug = tenantSlugHeader.ToString();

            if (!string.IsNullOrWhiteSpace(slug)) { return await tenantService.GetTenantBySlugAsync(slug); }
        }

        // Check for X-Tenant-Id header
        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader)) return null;

        var idString = tenantIdHeader.ToString();

        if (Guid.TryParse(idString, out var tenantId)) { return await tenantService.GetTenantByIdAsync(tenantId); }

        return null;
    }

    private async Task<Tenant?> ResolveTenantFromRoute(HttpContext context, ITenantService tenantService)
    {
        // Check for tenant slug in route values
        if (context.Request.RouteValues.TryGetValue("tenant", out var tenantRoute))
        {
            var slug = tenantRoute?.ToString();

            if (!string.IsNullOrWhiteSpace(slug)) { return await tenantService.GetTenantBySlugAsync(slug); }
        }

        // Check for tenant slug in query parameters
        if (!context.Request.Query.TryGetValue("tenant", out var tenantQuery)) return null;

        {
            var slug = tenantQuery.ToString();

            if (!string.IsNullOrWhiteSpace(slug)) { return await tenantService.GetTenantBySlugAsync(slug); }
        }

        return null;
    }

    private static bool IsIpAddress(string host) { return System.Net.IPAddress.TryParse(host, out _); }
}

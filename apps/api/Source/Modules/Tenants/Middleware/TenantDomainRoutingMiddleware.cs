using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Middleware to resolve tenant from HTTP host domain and set tenant context
///     Automatically detects tenant based on the request domain
/// </summary>
public class TenantDomainRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantDomainRoutingMiddleware> _logger;

    public TenantDomainRoutingMiddleware(
        RequestDelegate next,
        ILogger<TenantDomainRoutingMiddleware> _logger)
    {
        _next = next;
        this._logger = _logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantDomainsService tenantDomainsService,
        ITenantContext tenantContext)
    {
        try
        {
            // Get the host from the request
            var host = context.Request.Host.Host;

            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning("No host found in request");
                await _next(context);
                return;
            }

            _logger.LogDebug("Processing request for host: {Host}", host);

            // Parse the host to extract domain parts
            var hostParts = host.Split('.');

            string? topLevelDomain = null;
            string? subdomain = null;

            if (hostParts.Length >= 2)
            {
                // Extract TLD (e.g., "example.com")
                topLevelDomain = string.Join(".", hostParts.Skip(hostParts.Length - 2));

                // Extract subdomain if exists (e.g., "app" from "app.example.com")
                if (hostParts.Length > 2)
                {
                    subdomain = string.Join(".", hostParts.Take(hostParts.Length - 2));
                }
            }
            else if (hostParts.Length == 1)
            {
                // Single part domain (e.g., "localhost")
                topLevelDomain = host;
            }

            if (topLevelDomain == null)
            {
                _logger.LogWarning("Could not parse domain from host: {Host}", host);
                await _next(context);
                return;
            }

            // Find tenant by domain
            var tenant = await tenantDomainsService.FindTenantByDomainAsync(
                topLevelDomain,
                subdomain,
                context.RequestAborted);

            if (tenant != null)
            {
                // Set tenant context
                context.Items["TenantId"] = tenant.Id;
                context.Items["Tenant"] = tenant;

                _logger.LogInformation("Resolved tenant {TenantId} ({TenantName}) from domain {Domain}",
                    tenant.Id, tenant.Name, host);
            }
            else
            {
                _logger.LogDebug("No tenant found for domain: {Domain} (TLD: {TLD}, Subdomain: {Subdomain})",
                    host, topLevelDomain, subdomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant domain routing middleware");
        }

        await _next(context);
    }
}

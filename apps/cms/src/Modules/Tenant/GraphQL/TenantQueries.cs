using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.Tenant.Services;
using GameGuild.Modules.User.GraphQL;
using GameGuild.Common.Services;
using System.Security.Claims;

namespace GameGuild.Modules.Tenant.GraphQL;

/// <summary>
/// GraphQL queries for Tenant module
/// </summary>
[ExtendObjectType<Query>]
public class TenantQueries
{
    /// <summary>
    /// Get all tenants (non-deleted only)
    /// </summary>
    public async Task<IEnumerable<Models.Tenant>> GetTenants(
        [Service] ITenantService tenantService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        // Require authentication for tenant queries
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        return await tenantService.GetAllTenantsAsync();
    }

    /// <summary>
    /// Get a tenant by ID
    /// </summary>
    public async Task<Models.Tenant?> GetTenantById(
        [Service] ITenantService tenantService,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid id)
    {
        // Require authentication for tenant queries
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("Authentication required");        }

        return await tenantService.GetTenantByIdAsync(id);
    }

    /// <summary>
    /// Get soft-deleted tenants
    /// </summary>
    public async Task<IEnumerable<Models.Tenant>> GetDeletedTenants(
        [Service] ITenantService tenantService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        // Require authentication for tenant queries
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }        return await tenantService.GetDeletedTenantsAsync();
    }

    /// <summary>
    /// Get users in a tenant
    /// </summary>
    public async Task<IEnumerable<TenantPermission>> GetUsersInTenant(
        [Service] ITenantService tenantService,
        [Service] IHttpContextAccessor httpContextAccessor,
        Guid tenantId)
    {
        // Require authentication for tenant queries
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        return await tenantService.GetUsersInTenantAsync(tenantId);
    }
}

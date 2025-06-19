using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Modules.Tenant.Models;

namespace GameGuild.Modules.Tenant.Services;

/// <summary>
/// Service for managing tenant context in requests
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Get the current tenant ID from the request or claims
    /// </summary>
    Task<Guid?> GetCurrentTenantIdAsync(ClaimsPrincipal? user = null, string? tenantHeader = null);

    /// <summary>
    /// Get the current tenant entity 
    /// </summary>
    Task<Models.Tenant?> GetCurrentTenantAsync(ClaimsPrincipal? user = null, string? tenantHeader = null);

    /// <summary>
    /// Get permission data for user in the specified tenant
    /// </summary>
    Task<TenantPermission?> GetTenantPermissionAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Check if user has permission to access the specified tenant
    /// </summary>
    Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, Guid tenantId);
}

/// <summary>
/// Service implementation for tenant context management
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly ApplicationDbContext _context;

    private const string TenantClaim = "tenant_id";

    private const string TenantHeader = "X-Tenant-ID";

    public TenantContextService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get the current tenant ID from the request or claims
    /// </summary>
    public async Task<Guid?> GetCurrentTenantIdAsync(ClaimsPrincipal? user = null, string? tenantHeader = null)
    {
        // Check header first (highest priority)
        if (!string.IsNullOrEmpty(tenantHeader) && Guid.TryParse(tenantHeader, out Guid tenantHeaderId))
        {
            // Verify tenant exists
            if (await _context.Tenants.AnyAsync(t => t.Id == tenantHeaderId && t.IsActive))
            {
                return tenantHeaderId;
            }
        }

        // Check user claims
        if (user?.Identity?.IsAuthenticated == true)
        {
            Claim? tenantClaim = user.FindFirst(TenantClaim);
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out Guid tenantClaimId))
            {
                // Verify tenant exists
                if (await _context.Tenants.AnyAsync(t => t.Id == tenantClaimId && t.IsActive))
                {
                    return tenantClaimId;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get the current tenant entity 
    /// </summary>
    public async Task<Models.Tenant?> GetCurrentTenantAsync(ClaimsPrincipal? user = null, string? tenantHeader = null)
    {
        var tenantId = await GetCurrentTenantIdAsync(user, tenantHeader);
        if (!tenantId.HasValue)
        {
            return null;
        }

        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);
    }

    /// <summary>
    /// Get permission data for user in the specified tenant
    /// </summary>
    public async Task<TenantPermission?> GetTenantPermissionAsync(Guid userId, Guid tenantId)
    {
        return await _context.TenantPermissions
            .FirstOrDefaultAsync(tp =>
                tp.UserId == userId &&
                tp.TenantId == tenantId &&
                tp.IsValid
            );
    }

    /// <summary>
    /// Check if user has permission to access the specified tenant
    /// </summary>
    public async Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, Guid tenantId)
    {
        if (!user.Identity?.IsAuthenticated == true || string.IsNullOrEmpty(user.FindFirst(ClaimTypes.NameIdentifier)?.Value))
        {
            return false;
        }

        Guid userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        TenantPermission? permission = await GetTenantPermissionAsync(userId, tenantId);

        return permission != null;
    }
}

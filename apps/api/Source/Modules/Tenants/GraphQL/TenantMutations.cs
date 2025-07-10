using System.Security.Claims;
using GameGuild.Common;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// GraphQL mutations for Tenant module
/// </summary>
[ExtendObjectType<Mutation>]
public class TenantMutations {
  /// <summary>
  /// Create a new tenant
  /// </summary>
  public async Task<Tenant> CreateTenant(
    [Service] ITenantService tenantService,
    [Service] IPermissionService permissionService, [Service] IHttpContextAccessor httpContextAccessor,
    CreateTenantInput input
  ) {
    // Extract user ID and tenant ID from JWT token for permission check
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null) throw new UnauthorizedAccessException("No HTTP context available");

    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(userIdClaim, out var userId)) throw new UnauthorizedAccessException("Invalid or missing user ID in token");

    var tenantIdClaim = httpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;

    if (!Guid.TryParse(tenantIdClaim, out var tenantId)) throw new UnauthorizedAccessException("Invalid or missing tenant ID in token");

    // Check if user has permission to create tenants
    var hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Create);

    if (!hasPermission) throw new UnauthorizedAccessException("Insufficient permissions to create tenant");

    var tenant = new Tenant { Name = input.Name, Description = input.Description, IsActive = input.IsActive };

    return await tenantService.CreateTenantAsync(tenant);
  }

  /// <summary>
  /// Update an existing tenant
  /// </summary>
  public async Task<Tenant?> UpdateTenant([Service] ITenantService tenantService, UpdateTenantInput input) {
    var tenant = new Tenant { Id = input.Id, Name = input.Name ?? string.Empty, Description = input.Description, IsActive = input.IsActive ?? true };

    return await tenantService.UpdateTenantAsync(tenant);
  }

  /// <summary>
  /// Soft delete a tenant
  /// </summary>
  public async Task<bool> SoftDeleteTenant([Service] ITenantService tenantService, Guid id) { return await tenantService.SoftDeleteTenantAsync(id); }

  /// <summary>
  /// Restore a soft-deleted tenant
  /// </summary>
  public async Task<bool> RestoreTenant([Service] ITenantService tenantService, Guid id) { return await tenantService.RestoreTenantAsync(id); }

  /// <summary>
  /// Add a user to a tenant
  /// </summary>
  public async Task<TenantPermission> AddUserToTenant(
    [Service] ITenantService tenantService,
    AddUserToTenantInput input
  ) {
    return await tenantService.AddUserToTenantAsync(input.UserId, input.TenantId);
  }

  /// <summary>
  /// Remove a user from a tenant
  /// </summary>
  public async Task<bool> RemoveUserFromTenant([Service] ITenantService tenantService, RemoveUserFromTenantInput input) { return await tenantService.RemoveUserFromTenantAsync(input.UserId, input.TenantId); }
}

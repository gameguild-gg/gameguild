using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Data;
using GameGuild.Modules.Tenant.Models;

namespace GameGuild.Common.Services;

/// <summary>
/// Implementation of the three-layer permission service
/// Layer 1: Tenant-wide permissions with default support
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== LAYER 1: TENANT-WIDE PERMISSIONS =====

    public async Task<TenantPermission> GrantTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions)
    {
        if (permissions == null)
            throw new ArgumentNullException(nameof(permissions));

        if (permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        if (permissions.Length > TenantPermissionConstants.MaxPermissionsPerGrant)
            throw new ArgumentException($"Cannot grant more than {TenantPermissionConstants.MaxPermissionsPerGrant} permissions at once", nameof(permissions));

        // Find existing permission record or create new one
        TenantPermission? existingPermission = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

        TenantPermission tenantPermission;

        if (existingPermission != null)
        {
            // Update existing permission
            tenantPermission = existingPermission;
            foreach (PermissionType permission in permissions!)
            {
                tenantPermission.AddPermission(permission);
            }
            tenantPermission.Touch();
        }
        else
        {
            // Create new permission record
            tenantPermission = new TenantPermission
            {
                UserId = userId, TenantId = tenantId,
                // IsActive replaced by IsValid property,
                // JoinedAt replaced by CreatedAt (inherited)
                // Status removed - using IsValid property instead
            };

            foreach (PermissionType permission in permissions!)
            {
                tenantPermission.AddPermission(permission);
            }

            _context.TenantPermissions.Add(tenantPermission);
        }

        await _context.SaveChangesAsync();

        return tenantPermission;
    }

    public async Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission)
    {
        // For null user, only check default permissions
        if (!userId.HasValue)
        {
            return await CheckDefaultPermissionAsync(tenantId, permission);
        }        // 1. Check user-specific permissions first
        TenantPermission? userPermission = await _context.TenantPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId &&
                                       tp.DeletedAt == null && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
            );

        if (userPermission?.IsValid == true && userPermission.HasPermission(permission))
        {
            return true;
        }

        // 2. Check tenant default permissions
        if (tenantId.HasValue)
        {
            TenantPermission? tenantDefault = await _context.TenantPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == tenantId &&
                                           tp.DeletedAt == null && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                );

            if (tenantDefault?.IsValid == true && tenantDefault.HasPermission(permission))
            {
                return true;
            }
        }

        // 3. Check global default permissions
        TenantPermission? globalDefault = await _context.TenantPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null &&
                                       tp.DeletedAt == null && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
            );

        return globalDefault?.IsValid == true && globalDefault.HasPermission(permission);
    }    public async Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId)
    {
        TenantPermission? permission = await _context.TenantPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId &&
                                       tp.DeletedAt == null && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
            );

        if (permission?.IsValid != true)
            return [];

        return GetPermissionsFromEntity(permission);
    }

    public async Task RevokeTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions)
    {
        TenantPermission? tenantPermission = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

        if (tenantPermission == null)
            return;

        foreach (PermissionType permission in permissions)
        {
            tenantPermission.RemovePermission(permission);
        }

        tenantPermission.Touch();
        await _context.SaveChangesAsync();
    }

    // === DEFAULT PERMISSION MANAGEMENT ===

    public async Task<TenantPermission> SetTenantDefaultPermissionsAsync(Guid? tenantId, PermissionType[] permissions)
    {
        return await GrantTenantPermissionAsync(null, tenantId, permissions);
    }

    public async Task<TenantPermission> SetGlobalDefaultPermissionsAsync(PermissionType[] permissions)
    {
        return await GrantTenantPermissionAsync(null, null, permissions);
    }

    public async Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid? tenantId)
    {
        return await GetTenantPermissionsAsync(null, tenantId);
    }

    public async Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync()
    {
        return await GetTenantPermissionsAsync(null, null);
    }

    public async Task<IEnumerable<PermissionType>> GetEffectiveTenantPermissionsAsync(Guid userId, Guid? tenantId)
    {
        var effectivePermissions = new HashSet<PermissionType>();

        // 1. Start with global defaults
        var globalDefaults = await GetGlobalDefaultPermissionsAsync();
        foreach (PermissionType permission in globalDefaults)
        {
            effectivePermissions.Add(permission);
        }

        // 2. Add tenant defaults (if applicable)
        if (tenantId.HasValue)
        {
            var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId);
            foreach (PermissionType permission in tenantDefaults)
            {
                effectivePermissions.Add(permission);
            }
        }

        // 3. Add user-specific permissions (override/add to defaults)
        var userPermissions = await GetTenantPermissionsAsync(userId, tenantId);
        foreach (PermissionType permission in userPermissions)
        {
            effectivePermissions.Add(permission);
        }        return effectivePermissions;
    }

    // === USER-TENANT MEMBERSHIP FUNCTIONALITY ===

    public async Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(Guid userId)
    {
        return await _context.TenantPermissions
            .AsNoTracking()
            .Include(tp => tp.Tenant)
            .Where(tp => tp.UserId == userId && tp.TenantId != null &&
                         tp.DeletedAt == null && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
            )
            .ToListAsync();
    }

    public async Task<TenantPermission> JoinTenantAsync(Guid userId, Guid tenantId)
    {
        // Check if user is already a member
        TenantPermission? existingMembership = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);
        if (existingMembership != null)
        {
            // Reactivate if expired or deleted
            if (!existingMembership.IsValid)
            {
                existingMembership.ExpiresAt = null; // Remove expiration
                existingMembership.Restore(); // Undelete if deleted
                // Reset to minimal permissions only when reactivating
                existingMembership.PermissionFlags1 = 0UL; // Clear all permissions
                existingMembership.PermissionFlags2 = 0UL; // Clear all permissions
                foreach (PermissionType permission in TenantPermissionConstants.MinimalUserPermissions)
                {
                    existingMembership.AddPermission(permission);
                }

                existingMembership.Touch();
                await _context.SaveChangesAsync();
            }

            return existingMembership;
        }

        // Create new membership with minimal permissions
        var membership = new TenantPermission
        {
            UserId = userId, TenantId = tenantId,
            // IsActive replaced by IsValid property,
            // JoinedAt replaced by CreatedAt (inherited)
            // Status removed - using IsValid property instead
        };

        // Grant minimal permissions
        foreach (PermissionType permission in TenantPermissionConstants.MinimalUserPermissions)
        {
            membership.AddPermission(permission);
        }

        _context.TenantPermissions.Add(membership);
        await _context.SaveChangesAsync();

        return membership;
    }

    public async Task LeaveTenantAsync(Guid userId, Guid tenantId)
    {
        TenantPermission? membership = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

        if (membership != null)
        {
            // Instead of setting Status, we expire the membership or delete it
            membership.ExpiresAt = DateTime.UtcNow; // Expire immediately
            membership.Touch();
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId)
    {
        TenantPermission? membership = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

        return membership?.IsActiveMembership == true;
    }

    public async Task<TenantPermission> UpdateTenantMembershipExpirationAsync(Guid userId, Guid tenantId, DateTime? expiresAt)
    {
        TenantPermission? membership = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

        if (membership == null)
            throw new InvalidOperationException("User is not a member of this tenant");

        membership.ExpiresAt = expiresAt;
        membership.Touch();

        await _context.SaveChangesAsync();

        return membership;
    }

    // ===== LAYER 2: CONTENT-TYPE-WIDE PERMISSIONS =====

    public async Task GrantContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions)
    {
        if (string.IsNullOrWhiteSpace(contentTypeName))
            throw new ArgumentException("Content type name cannot be null or empty", nameof(contentTypeName));
        if (permissions?.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        // Find existing permission record or create new one
        ContentTypePermission? existingPermission = await _context.ContentTypePermissions
            .FirstOrDefaultAsync(ctp => ctp.UserId == userId &&
                                        ctp.TenantId == tenantId &&
                                        ctp.ContentType == contentTypeName &&
                                        ctp.DeletedAt == null
            );

        ContentTypePermission contentTypePermission;

        if (existingPermission != null)
        {
            // Update existing permission
            contentTypePermission = existingPermission;
            foreach (PermissionType permission in permissions!)
            {
                contentTypePermission.AddPermission(permission);
            }
            contentTypePermission.Touch();
        }
        else
        {
            // Create new permission record
            contentTypePermission = new ContentTypePermission
            {
                UserId = userId, TenantId = tenantId, ContentType = contentTypeName,
                // AssignedAt replaced by CreatedAt (inherited)
                // AssignedByUserId removed - will be tracked through permission logs
                // IsActive replaced by IsValid property
            };

            // Add permissions
            foreach (PermissionType permission in permissions!)
            {
                contentTypePermission.AddPermission(permission);
            }

            _context.ContentTypePermissions.Add(contentTypePermission);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasContentTypePermissionAsync(Guid userId, Guid? tenantId, string contentTypeName, PermissionType permission)
    {
        if (string.IsNullOrWhiteSpace(contentTypeName))
            return false;

        // Check user-specific content type permission
        ContentTypePermission? userPermission = await _context.ContentTypePermissions
            .FirstOrDefaultAsync(ctp => ctp.UserId == userId &&
                                        ctp.TenantId == tenantId &&
                                        ctp.ContentType == contentTypeName &&
                                        ctp.DeletedAt == null && (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
            );

        if (userPermission?.HasPermission(permission) == true)
            return true; // Check tenant default for this content type
        if (tenantId.HasValue)
        {
            ContentTypePermission? tenantDefault = await _context.ContentTypePermissions
                .FirstOrDefaultAsync(ctp => ctp.UserId == null &&
                                            ctp.TenantId == tenantId &&
                                            ctp.ContentType == contentTypeName &&
                                            ctp.DeletedAt == null && (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
                );

            if (tenantDefault?.HasPermission(permission) == true)
                return true;
        }

        // Check global default for this content type
        ContentTypePermission? globalDefault = await _context.ContentTypePermissions
            .FirstOrDefaultAsync(ctp => ctp.UserId == null &&
                                        ctp.TenantId == null &&
                                        ctp.ContentType == contentTypeName &&
                                        ctp.DeletedAt == null && (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
            );

        if (globalDefault?.HasPermission(permission) == true)
            return true;

        // If content-type specific permissions don't exist, fall back to tenant-level permissions
        // This implements the permission hierarchy where tenant permissions apply to all content types
        if (tenantId.HasValue)
        {
            return await HasTenantPermissionAsync(userId, tenantId, permission);
        }

        // If nothing found, check global tenant permissions as last resort
        return await CheckDefaultPermissionAsync(null, permission);
    }

    public async Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(Guid? userId, Guid? tenantId, string contentTypeName)
    {
        if (string.IsNullOrWhiteSpace(contentTypeName))
            return [];

        ContentTypePermission? contentTypePermission = await _context.ContentTypePermissions
            .FirstOrDefaultAsync(ctp => ctp.UserId == userId &&
                                        ctp.TenantId == tenantId &&
                                        ctp.ContentType == contentTypeName &&
                                        ctp.DeletedAt == null && (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
            );

        if (contentTypePermission == null)
            return [];

        return GetPermissionsFromContentTypeEntity(contentTypePermission);
    }

    public async Task RevokeContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions)
    {
        if (string.IsNullOrWhiteSpace(contentTypeName))
            throw new ArgumentException("Content type name cannot be null or empty", nameof(contentTypeName));

        if (permissions?.Length == 0)
            return; // Nothing to revoke

        ContentTypePermission? existingPermission = await _context.ContentTypePermissions
            .FirstOrDefaultAsync(ctp => ctp.UserId == userId &&
                                        ctp.TenantId == tenantId &&
                                        ctp.ContentType == contentTypeName &&
                                        ctp.DeletedAt == null
            );

        if (existingPermission != null)
        {
            foreach (PermissionType permission in permissions!)
            {
                existingPermission.RemovePermission(permission);
            }
            existingPermission.Touch();
            await _context.SaveChangesAsync();
        }
    }

    private static IEnumerable<PermissionType> GetPermissionsFromContentTypeEntity(ContentTypePermission permission)
    {
        var permissions = new List<PermissionType>();

        // Check all possible permission types, using distinct values to avoid duplicates like Delete/SoftDelete
        foreach (PermissionType permissionType in Enum.GetValues<PermissionType>().Distinct())
        {
            if (permission.HasPermission(permissionType))
            {
                permissions.Add(permissionType);
            }
        }

        return permissions;
    }

    // === PRIVATE HELPER METHODS ===

    private async Task<bool> CheckDefaultPermissionAsync(Guid? tenantId, PermissionType permission)
    {        // Check tenant default
        if (tenantId.HasValue)
        {
            TenantPermission? tenantDefault = await _context.TenantPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == tenantId &&
                                           tp.ExpiresAt == null && tp.DeletedAt == null
                );

            if (tenantDefault?.IsValid == true && tenantDefault.HasPermission(permission))
            {
                return true;
            }
        }

        // Check global default
        TenantPermission? globalDefault = await _context.TenantPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null &&
                                       tp.ExpiresAt == null && tp.DeletedAt == null
            );

        return globalDefault?.IsValid == true && globalDefault.HasPermission(permission);
    }

    private static IEnumerable<PermissionType> GetPermissionsFromEntity(TenantPermission permission)
    {
        var permissions = new List<PermissionType>();

        // Check all possible permission types, using distinct values to avoid duplicates like Delete/SoftDelete
        foreach (PermissionType permissionType in Enum.GetValues<PermissionType>().Distinct())
        {
            if (permission.HasPermission(permissionType))
            {
                permissions.Add(permissionType);
            }
        }

        return permissions;
    }

    // ===== HELPER METHODS =====

    public async Task<Guid?> GetResourceTenantIdAsync(Guid resourceId)
    {
        // TODO: Implement when we have concrete resource entities
        // This will need to query the specific resource tables to find tenant ID
        await Task.CompletedTask;

        return null;
    }

    public async Task<string?> GetResourceContentTypeAsync(Guid resourceId)
    {
        // TODO: Implement when we have concrete resource entities
        // This will need to determine the content type based on the resource
        await Task.CompletedTask;

        return null;
    }

    // ===== LAYER 3: RESOURCE-ENTRY PERMISSIONS IMPLEMENTATION =====

    public async Task GrantResourcePermissionAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : BaseEntity
    {
        TPermission? existingPermission = await _context.Set<TPermission>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId);

        if (existingPermission != null)
        {
            // Update existing permissions
            foreach (PermissionType permission in permissions)
            {
                existingPermission.AddPermission(permission);
            }
            existingPermission.Touch();
        }
        else
        {
            // Create new permission record
            var newPermission = new TPermission
            {
                UserId = userId, TenantId = tenantId, ResourceId = resourceId
            };

            foreach (PermissionType permission in permissions)
            {
                newPermission.AddPermission(permission);
            }

            _context.Set<TPermission>().Add(newPermission);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permission)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity
    {
        TPermission? resourcePermission = await _context.Set<TPermission>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId && p.ExpiresAt == null && p.DeletedAt == null);

        return resourcePermission?.HasPermission(permission) ?? false;
    }

    public async Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId) where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity
    {
        TPermission? resourcePermission = await _context.Set<TPermission>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId && p.ExpiresAt == null && p.DeletedAt == null);

        if (resourcePermission == null) return [];

        var permissions = new HashSet<PermissionType>();
        foreach (PermissionType permissionType in Enum.GetValues<PermissionType>())
        {
            if (resourcePermission.HasPermission(permissionType))
            {
                permissions.Add(permissionType);
            }
        }

        return permissions;
    }

    public async Task RevokeResourcePermissionAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity
    {
        TPermission? existingPermission = await _context.Set<TPermission>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId);

        if (existingPermission != null)
        {
            foreach (PermissionType permission in permissions)
            {
                existingPermission.RemovePermission(permission);
            }
            existingPermission.Touch();
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<Guid, IEnumerable<PermissionType>>> GetBulkResourcePermissionsAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid[] resourceIds)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity
    {
        var permissions = await _context.Set<TPermission>()
            .Where(p => p.UserId == userId && p.TenantId == tenantId && resourceIds.Contains(p.ResourceId) && p.ExpiresAt == null && p.DeletedAt == null)
            .ToListAsync();

        var result = new Dictionary<Guid, IEnumerable<PermissionType>>();

        foreach (TPermission permission in permissions)
        {
            var permissionTypes = new List<PermissionType>();
            foreach (PermissionType permType in Enum.GetValues<PermissionType>())
            {
                if (permission.HasPermission(permType))
                {
                    permissionTypes.Add(permType);
                }
            }
            result[permission.ResourceId] = permissionTypes;
        }

        return result;
    }

    public async Task ShareResourceAsync<TPermission, TResource>(Guid resourceId, Guid targetUserId, Guid? tenantId, PermissionType[] permissions, DateTime? expiresAt = null)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : BaseEntity
    {
        await GrantResourcePermissionAsync<TPermission, TResource>(
            userId: targetUserId,
            tenantId: tenantId,
            resourceId: resourceId,
            permissions: permissions
        );

        // Set expiration if provided
        if (expiresAt.HasValue)
        {
            TPermission? permission = await _context.Set<TPermission>()
                .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.TenantId == tenantId && p.ResourceId == resourceId);

            if (permission != null)
            {
                permission.ExpiresAt = expiresAt.Value;
                permission.Touch();
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(Guid[] userIds, Guid tenantId, PermissionType[] permissions)
    {
        if (userIds == null || userIds.Length == 0)
            throw new ArgumentException("At least one user ID must be specified", nameof(userIds));

        if (permissions == null)
            throw new ArgumentNullException(nameof(permissions));

        if (permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        if (permissions.Length > TenantPermissionConstants.MaxPermissionsPerGrant)
            throw new ArgumentException($"Cannot grant more than {TenantPermissionConstants.MaxPermissionsPerGrant} permissions at once", nameof(permissions));

        // Get existing permissions for all users in this tenant
        var existingPermissions = await _context.TenantPermissions
            .Where(tp => userIds.Contains(tp.UserId!.Value) && tp.TenantId == tenantId && tp.DeletedAt == null)
            .ToListAsync();

        var existingPermissionLookup = existingPermissions.ToDictionary(ep => ep.UserId!.Value);
        var result = new List<TenantPermission>();

        foreach (Guid userId in userIds)
        {
            TenantPermission tenantPermission;

            if (existingPermissionLookup.TryGetValue(userId, out TenantPermission? existingPermission))
            {
                // Update existing permission
                tenantPermission = existingPermission;
                foreach (PermissionType permission in permissions)
                {
                    tenantPermission.AddPermission(permission);
                }
                tenantPermission.Touch();
            }
            else
            {
                // Create new permission record
                tenantPermission = new TenantPermission
                {
                    UserId = userId, TenantId = tenantId,
                };

                foreach (PermissionType permission in permissions)
                {
                    tenantPermission.AddPermission(permission);
                }

                _context.TenantPermissions.Add(tenantPermission);
            }

            result.Add(tenantPermission);
        }

        // Single SaveChangesAsync for all operations
        await _context.SaveChangesAsync();

        return result;
    }
}

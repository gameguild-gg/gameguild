using GameGuild.Permissions.Domain.Entities;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Core permission service interface
/// </summary>
public interface IPermissionService
{
    // Tenant Permission Management
    Task<TenantPermission> GrantTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[ ] permissions,
        Guid? grantedBy = null,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default
    );

    Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(Guid[ ] userIds, Guid tenantId, string[ ] permissions, Guid? grantedBy = null, CancellationToken cancellationToken = default);

    Task<bool> RevokeTenantPermissionAsync(Guid? userId, Guid? tenantId, string[ ] permissions, CancellationToken cancellationToken = default);

    Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, string permission, CancellationToken cancellationToken = default);

    Task<List<string>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<string>> GetEffectivePermissionsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    // Tenant Membership
    Task<TenantPermission> JoinTenantAsync(Guid userId, Guid tenantId, Guid? invitedBy = null, CancellationToken cancellationToken = default);

    Task<bool> LeaveTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    // Default Permissions
    Task<List<string>> GetGlobalDefaultPermissionsAsync(CancellationToken cancellationToken = default);

    Task SetGlobalDefaultPermissionsAsync(string[ ] permissions, Guid? setBy = null, CancellationToken cancellationToken = default);

    Task<List<string>> GetTenantDefaultPermissionsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task SetTenantDefaultPermissionsAsync(Guid tenantId, string[ ] permissions, Guid? setBy = null, CancellationToken cancellationToken = default);
}

/// <summary>
///     Cached permission service for performance
/// </summary>
public interface ICachedPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string permission, CancellationToken cancellationToken = default);

    Task<List<string>> GetPermissionsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task InvalidateCacheAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task InvalidateTenantCacheAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Permission template service interface
/// </summary>
public interface IPermissionTemplateService
{
    Task<PermissionTemplate> CreateTemplateAsync(PermissionTemplate template, CancellationToken cancellationToken = default);

    Task<PermissionTemplate> UpdateTemplateAsync(PermissionTemplate template, CancellationToken cancellationToken = default);

    Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<PermissionTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<PermissionTemplate?> GetTemplateByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplate>> GetTemplatesAsync(string? category = null, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default);

    Task<TenantPermission> ApplyTemplateToUserAsync(Guid templateId, Guid userId, Guid tenantId, Guid? appliedBy = null, CancellationToken cancellationToken = default);

    Task<int> BulkApplyTemplateAsync(Guid templateId, Guid[ ] userIds, Guid tenantId, Guid? appliedBy = null, CancellationToken cancellationToken = default);

    Task<List<string>> GetTemplatePermissionsAsync(Guid templateId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for TenantPermission
/// </summary>
public interface ITenantPermissionRepository
{
    Task<TenantPermission> CreateAsync(TenantPermission permission, CancellationToken cancellationToken = default);

    Task<TenantPermission> UpdateAsync(TenantPermission permission, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenantPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenantPermission?> GetByUserAndTenantAsync(Guid? userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<TenantPermission>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<List<TenantPermission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<TenantPermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Repository interface for PermissionTemplate
/// </summary>
public interface IPermissionTemplateRepository
{
    Task<PermissionTemplate> CreateAsync(PermissionTemplate template, CancellationToken cancellationToken = default);

    Task<PermissionTemplate> UpdateAsync(PermissionTemplate template, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PermissionTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PermissionTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplate>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplate>> GetByCategoryAsync(string category, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<List<PermissionTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default);
}

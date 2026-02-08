using GameGuild.Identity.Authorization;
using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Base abstract class for all permission entities
///     Provides common functionality for managing permissions with tenant support
///     Implements the foundation for the 3-layer permission system
/// </summary>
public abstract class WithPermissions : EntityBase<Guid>
{
    /// <summary>
    ///     Protected constructor for entity framework
    /// </summary>
    protected WithPermissions() { }

    /// <summary>
    ///     Constructor for creating permissions
    /// </summary>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID (null for global permissions)</param>
    // ReSharper disable once VirtualMemberCallInConstructor - TenantId is effectively sealed in this hierarchy
#pragma warning disable CA2214 // Do not call overridable methods in constructors
    protected WithPermissions(Guid? userId, Guid? tenantId)
    {
        UserId = userId;
        TenantId = tenantId.HasValue ? new TenantId(tenantId.Value) : null;
        GrantedAt = DateTime.UtcNow;
    }
#pragma warning restore CA2214

    /// <summary>
    ///     User ID to whom the permissions are granted (null for default permissions)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Serialized permissions as a comma-separated string
    ///     Stores the actual permission values efficiently
    /// </summary>
    public string Permissions { get; set; } = string.Empty;

    /// <summary>
    ///     Date and time when these permissions expire (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Whether these permissions are currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Notes or comments about these permissions
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     User ID who granted these permissions
    /// </summary>
    public Guid? GrantedBy { get; set; }

    /// <summary>
    ///     Date and time when these permissions were granted
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Add a permission to this entity
    /// </summary>
    /// <param name="permission">Permission to add</param>
    public void AddPermission(PermissionType permission)
    {
        var permissions = GetPermissionsAsEnum().ToList();

        if (!permissions.Contains(permission))
        {
            permissions.Add(permission);
            Permissions = string.Join(",", permissions.Select(p => (int) p));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Remove a permission from this entity
    /// </summary>
    /// <param name="permission">Permission to remove</param>
    public void RemovePermission(PermissionType permission)
    {
        var permissions = GetPermissionsAsEnum().ToList();

        if (permissions.Contains(permission))
        {
            permissions.Remove(permission);
            Permissions = string.Join(",", permissions.Select(p => (int) p));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Check if this entity has a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if permission exists</returns>
    public bool HasPermission(PermissionType permission) { return GetPermissionsAsEnum().Contains(permission); }

    /// <summary>
    ///     Get all permissions as enumeration values
    /// </summary>
    /// <returns>Collection of permission types</returns>
    public IEnumerable<PermissionType> GetPermissionsAsEnum()
    {
        if (string.IsNullOrWhiteSpace(Permissions)) return [];

        return Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => int.TryParse(p.Trim(), out var val) ? (PermissionType) val : (PermissionType?) null).Where(p => p.HasValue).Select(p => p!.Value);
    }

    /// <summary>
    ///     Set permissions from enumeration values
    /// </summary>
    /// <param name="permissions">GameGuild.Permissions to set</param>
    public void SetPermissions(IEnumerable<PermissionType> permissions)
    {
        Permissions = string.Join(",", permissions.Select(p => (int) p));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if permissions have expired
    /// </summary>
    /// <returns>True if permissions are expired</returns>
    public bool IsExpired() { return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow; }

    /// <summary>
    ///     Check if permissions are currently effective (active and not expired)
    /// </summary>
    /// <returns>True if permissions are effective</returns>
    public bool IsEffective() { return IsActive && !IsExpired(); }

    /// <summary>
    ///     Expire these permissions
    /// </summary>
    public void Expire()
    {
        IsActive = false;
        ExpiresAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Extend the expiration date
    /// </summary>
    /// <param name="newExpirationDate">New expiration date (null for permanent)</param>
    public void ExtendExpiration(DateTime? newExpirationDate)
    {
        ExpiresAt = newExpirationDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Add multiple permissions at once
    /// </summary>
    /// <param name="permissions">GameGuild.Permissions to add</param>
    public void AddPermissions(IEnumerable<PermissionType> permissions)
    {
        var existingPermissions = GetPermissionsAsEnum().ToList();
        var newPermissions = permissions.Where(p => !existingPermissions.Contains(p)).ToList();

        if (newPermissions.Count > 0)
        {
            existingPermissions.AddRange(newPermissions);
            SetPermissions(existingPermissions);
        }
    }

    /// <summary>
    ///     Remove multiple permissions at once
    /// </summary>
    /// <param name="permissions">GameGuild.Permissions to remove</param>
    public void RemovePermissions(IEnumerable<PermissionType> permissions)
    {
        var existingPermissions = GetPermissionsAsEnum().ToList();
        var hasChanges = false;

        foreach (var permission in permissions)
        {
            if (existingPermissions.Contains(permission))
            {
                existingPermissions.Remove(permission);
                hasChanges = true;
            }
        }

        if (hasChanges) { SetPermissions(existingPermissions); }
    }
}

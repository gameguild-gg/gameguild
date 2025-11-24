namespace GameGuild.Authentication.Entities;

/// <summary>
///     Represents a role that can be assigned to users
///     Roles contain sets of permissions and can be used for RBAC (Role-Based Access Control)
/// </summary>
public class Role : EntityBase<Guid>
{
    /// <summary>
    ///     Default parameterless constructor (required by Entity Framework)
    /// </summary>
    public Role() { }

    /// <summary>
    ///     Constructor for creating a role
    /// </summary>
    /// <param name="name">Role name (must be unique within tenant)</param>
    /// <param name="description">Role description</param>
    /// <param name="tenantId">Tenant ID (null for global roles)</param>
    public Role(string name, string description, Guid? tenantId = null)
    {
        Name = name;
        Description = description;
        TenantId = tenantId;
        IsActive = true;
        Permissions = "[]"; // Initialize with empty JSON array
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Role name (unique within tenant)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Role description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     JSON array of permission strings
    ///     Example: ["users:read", "users:write", "posts:*"]
    /// </summary>
    public string Permissions { get; set; } = "[]";

    /// <summary>
    ///     Whether this role is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     Check if this is a global/system role
    /// </summary>
    /// <returns>True if this is a global role</returns>
    public bool IsGlobalRole() => TenantId == null;

    /// <summary>
    ///     Check if this is a tenant-specific role
    /// </summary>
    /// <returns>True if this is a tenant-specific role</returns>
    public bool IsTenantRole() => TenantId != null;
}

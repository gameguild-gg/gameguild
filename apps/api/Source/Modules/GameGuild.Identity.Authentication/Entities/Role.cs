using System.ComponentModel.DataAnnotations;
using GameGuild.Entities;

namespace GameGuild.Identity.Authentication;

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
    // ReSharper disable once VirtualMemberCallInConstructor - TenantId assignment is safe for this entity hierarchy
#pragma warning disable CA2214 // Do not call overridable methods in constructors
    public Role(string name, string description, Guid? tenantId = null)
    {
        Name = name;
        Description = description;
        TenantId = tenantId;
        IsActive = true;
        Permissions = "[]"; // Initialize with empty JSON array
    }
#pragma warning restore CA2214

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
    [MaxLength(4000)]
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

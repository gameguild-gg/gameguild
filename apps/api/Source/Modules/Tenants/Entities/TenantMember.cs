using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Represents a user's membership in a tenant with role and status tracking.
///     Links users to tenants with specific roles and membership lifecycle management.
///     Supports hierarchical relationships for organizational structures.
/// </summary>
[Table("tenant_members")]
[Index(nameof(UserId), nameof(TenantId), IsUnique = true)]
[Index(nameof(TenantId), nameof(IsActive))]
[Index(nameof(JoinedAt))]
[Index(nameof(ParentMemberId))]
public class TenantMember : EntityBase, ITenantable
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TenantMember() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial tenant member data</param>
    public TenantMember(object partial) : base(partial) { }

    /// <summary>
    ///     ID of the user who is a member
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     ID of the tenant the user belongs to
    /// </summary>
    [Required]
    public override Guid TenantId { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public override Tenant? Tenant { get; set; }

    /// <summary>
    ///     ID of the parent member in the hierarchy (null for root members)
    /// </summary>
    public Guid? ParentMemberId { get; set; }

    /// <summary>
    ///     Navigation property to the parent member
    /// </summary>
    [ForeignKey(nameof(ParentMemberId))]
    public TenantMember? ParentMember { get; set; }

    /// <summary>
    ///     Child members in the hierarchy
    /// </summary>
    public ICollection<TenantMember> ChildMembers { get; set; } = new List<TenantMember>();

    /// <summary>
    ///     Hierarchy path (e.g., "parent-id/this-id" for tracking full hierarchy)
    /// </summary>
    [MaxLength(2000)]
    public string? HierarchyPath { get; set; }

    /// <summary>
    ///     Level in the hierarchy (0 for root, 1 for direct children, etc.)
    /// </summary>
    public int HierarchyLevel { get; set; } = 0;

    /// <summary>
    ///     Primary role of the member in the tenant
    /// </summary>
    [MaxLength(100)]
    public string? Role { get; set; }

    /// <summary>
    ///     Whether this membership is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     When the user joined the tenant
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the user left the tenant (null if still active)
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    ///     Reason for leaving (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? LeaveReason { get; set; }

    /// <summary>
    ///     Custom member-specific settings (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? MemberSettings { get; set; }

    /// <summary>
    ///     Activate the membership
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LeftAt = null;
        LeaveReason = null;
        Touch();
    }

    /// <summary>
    ///     Deactivate/leave the membership
    /// </summary>
    /// <param name="reason">Reason for leaving</param>
    public void Leave(string? reason = null)
    {
        IsActive = false;
        LeftAt = DateTime.UtcNow;
        LeaveReason = reason;
        Touch();
    }

    /// <summary>
    ///     Update the member's role
    /// </summary>
    /// <param name="newRole">New role to assign</param>
    public void UpdateRole(string newRole)
    {
        Role = newRole;
        Touch();
    }

    /// <summary>
    ///     Checks if the membership is currently valid
    /// </summary>
    /// <returns>True if active and not left</returns>
    public bool IsValid()
    {
        return IsActive && LeftAt == null;
    }
}

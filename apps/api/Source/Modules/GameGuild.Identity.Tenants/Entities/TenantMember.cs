using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Represents a user's membership in a tenant with role and status tracking.
///     Links users to tenants with specific roles and membership lifecycle management.
///     Supports hierarchical relationships for organizational structures.
/// </summary>
/// <remarks>
///     <para>
///         <b>Cross-Module Relationship:</b> This entity references <c>User</c> from
///         <c>GameGuild.Users</c> module via <see cref="UserId"/>.
///     </para>
///     <para>
///         To maintain module decoupling, the <c>User</c> entity does not have a navigation
///         property back to this entity. Use <c>ITenantMemberRepository.GetByUserIdAsync(userId)</c>
///         to query all tenant memberships for a user.
///     </para>
///     <para>
///         See also: <c>GameGuild.Identity.Users.Entities.User</c>
///     </para>
/// </remarks>
[Table("TenantMembers")]
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
    public new Guid TenantId { get; set; }

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
    ///     Collection of child members in the hierarchy
    /// </summary>
    public ICollection<TenantMember> ChildMembers { get; set; } = new List<TenantMember>();

    /// <summary>
    ///     Role of the member within the tenant
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this membership is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     When the user joined the tenant
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the user left the tenant (null if still a member)
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    ///     Reason for leaving (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? LeaveReason { get; set; }

    /// <summary>
    ///     Additional metadata for the membership
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; set; }

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
    ///     Deactivate the membership
    /// </summary>
    public void Deactivate(string? reason = null)
    {
        IsActive = false;
        LeftAt = DateTime.UtcNow;
        LeaveReason = reason;
        Touch();
    }

    /// <summary>
    ///     Update the member's role
    /// </summary>
    public void UpdateRole(string newRole)
    {
        Role = newRole;
        Touch();
    }

    /// <summary>
    ///     Set the parent member for hierarchy
    /// </summary>
    public void SetParent(Guid? parentMemberId)
    {
        ParentMemberId = parentMemberId;
        Touch();
    }
}

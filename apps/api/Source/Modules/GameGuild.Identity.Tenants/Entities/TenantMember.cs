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
///         <c>GameGuild.Identity.Users</c> module via <see cref="UserId"/>. The <c>User</c> entity
///         has a navigation property <c>TenantMemberships</c> back to this entity for bidirectional
///         traversal when needed.
///     </para>
///     <para>
///         <b>Hierarchical Structure:</b> The <see cref="ParentMemberId"/>, <see cref="ParentMember"/>,
///         and <see cref="ChildMembers"/> properties enable organizational hierarchy within a tenant
///         (e.g., teams, departments, reporting chains). This hierarchy is for <b>organizational purposes only</b>
///         and does <b>NOT affect permission inheritance</b>. Each member's permissions are determined solely
///         by their assigned <see cref="Role"/> and any direct permission grants.
///     </para>
///     <para>
///         <b>Hierarchy Use Cases:</b>
///         <list type="bullet">
///             <item>Team structures: Team Lead → Team Members</item>
///             <item>Department organization: Department Head → Managers → Employees</item>
///             <item>Project hierarchies: Project Owner → Contributors</item>
///             <item>Approval workflows: Manager approval chains</item>
///         </list>
///         To traverse the hierarchy, use the navigation properties or query via <c>ParentMemberId</c>.
///     </para>
///     <para>
///         <b>Important:</b> Changing a member's position in the hierarchy (via <see cref="SetParent"/>)
///         does not grant or revoke any permissions. To change permissions, update the <see cref="Role"/>
///         property or manage direct permission grants separately.
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
    ///     ID of the parent member in the organizational hierarchy (null for root/top-level members).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property establishes organizational hierarchy within a tenant for purposes such as:
    ///         team structures, reporting chains, department organization, and approval workflows.
    ///     </para>
    ///     <para>
    ///         <b>Permission Behavior:</b> The hierarchy does <b>NOT</b> affect permission inheritance.
    ///         A child member does not automatically inherit permissions from their parent member.
    ///         Each member's permissions are determined independently by their <see cref="Role"/> and
    ///         any direct permission grants configured in the authorization system.
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> If a Team Lead has "reports:write" permission and a Team Member is their
    ///         child in the hierarchy, the Team Member does <b>NOT</b> automatically get "reports:write".
    ///         The Team Member's permissions come from their own role (e.g., "Member" role permissions).
    ///     </para>
    /// </remarks>
    /// <seealso cref="ParentMember"/>
    /// <seealso cref="ChildMembers"/>
    /// <seealso cref="SetParent"/>
    public Guid? ParentMemberId { get; set; }

    /// <summary>
    ///     Navigation property to the parent member in the organizational hierarchy.
    /// </summary>
    /// <remarks>
    ///     Use this property to navigate up the organizational hierarchy (e.g., finding a member's
    ///     manager, team lead, or department head). Returns null for top-level/root members.
    /// </remarks>
    /// <seealso cref="ParentMemberId"/>
    /// <seealso cref="ChildMembers"/>
    [ForeignKey(nameof(ParentMemberId))]
    public TenantMember? ParentMember { get; set; }

    /// <summary>
    ///     Collection of child members in the organizational hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this property to navigate down the organizational hierarchy (e.g., finding
    ///         all members reporting to a manager, all team members under a team lead).
    ///     </para>
    ///     <para>
    ///         This is for organizational purposes only and does <b>NOT</b> imply permission
    ///         delegation or inheritance. Child members have their own independent permissions
    ///         based on their assigned roles.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ParentMemberId"/>
    /// <seealso cref="ParentMember"/>
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
    ///     Set the parent member for organizational hierarchy.
    /// </summary>
    /// <param name="parentMemberId">
    ///     The ID of the parent member, or null to make this member a root/top-level member.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method changes the member's position in the organizational hierarchy.
    ///         Use this for team reassignments, department transfers, or reporting structure changes.
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> Changing the parent does <b>NOT</b> affect the member's permissions.
    ///         To change permissions, use <see cref="UpdateRole"/> or manage direct permission grants
    ///         through the authorization system. Permission inheritance through hierarchy is not supported.
    ///     </para>
    ///     <para>
    ///         <b>Validation:</b> Callers should ensure the parent member ID exists within the same
    ///         tenant and that setting this parent would not create a circular reference in the hierarchy.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ParentMemberId"/>
    /// <seealso cref="UpdateRole"/>
    public void SetParent(Guid? parentMemberId)
    {
        ParentMemberId = parentMemberId;
        Touch();
    }
}

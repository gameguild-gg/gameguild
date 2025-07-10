using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants.Entities;

/// <summary>
/// Represents a user group within a tenant (e.g., Students, Professors, Administrators)
/// </summary>
[Table("TenantUserGroups")]
[Index(nameof(TenantId), nameof(Name), IsUnique = true)]
public class TenantUserGroup : Entity {
  /// <summary>
  /// Name of the user group (e.g., "Students", "Professors", "Administrators")
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of what this group represents
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// ID of the tenant this group belongs to
  /// </summary>
  [Required]
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the parent group (for nested group hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Whether this is the default group for auto-assignment in this tenant
  /// </summary>
  public bool IsDefault { get; set; } = false;

  /// <summary>
  /// Navigation property to the tenant
  /// </summary>
  [ForeignKey(nameof(TenantId))]
  public override Tenant? Tenant { get; set; }

  /// <summary>
  /// Navigation property to the parent group
  /// </summary>
  [ForeignKey(nameof(ParentGroupId))]
  public virtual TenantUserGroup? ParentGroup { get; set; }

  /// <summary>
  /// Navigation property to sub-groups
  /// </summary>
  [InverseProperty(nameof(ParentGroup))]
  public virtual ICollection<TenantUserGroup> SubGroups { get; set; } = new List<TenantUserGroup>();

  /// <summary>
  /// Navigation property to user memberships in this group
  /// </summary>
  public virtual ICollection<TenantUserGroupMembership> Memberships { get; set; } = new List<TenantUserGroupMembership>();

  /// <summary>
  /// Navigation property to domains associated with this group
  /// </summary>
  public virtual ICollection<TenantDomain> Domains { get; set; } = new List<TenantDomain>();

  /// <summary>
  /// Default constructor
  /// </summary>
  public TenantUserGroup() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial tenant user group data</param>
  public TenantUserGroup(object partial) : base(partial) { }

  /// <summary>
  /// Activate the group
  /// </summary>
  public void Activate() {
    IsActive = true;
    Touch();
  }

  /// <summary>
  /// Deactivate the group
  /// </summary>
  public void Deactivate() {
    IsActive = false;
    Touch();
  }

  /// <summary>
  /// Get the full hierarchy path of this group
  /// </summary>
  /// <returns>Array of group names from root to this group</returns>
  public string[] GetHierarchyPath() {
    var path = new List<string>();
    var current = this;

    while (current != null) {
      path.Insert(0, current.Name);
      current = current.ParentGroup;
    }

    return path.ToArray();
  }

  /// <summary>
  /// Get all descendant groups (sub-groups and their sub-groups recursively)
  /// </summary>
  /// <returns>Collection of all descendant groups</returns>
  public IEnumerable<TenantUserGroup> GetAllDescendants() {
    var descendants = new List<TenantUserGroup>();

    foreach (var subGroup in SubGroups) {
      descendants.Add(subGroup);
      descendants.AddRange(subGroup.GetAllDescendants());
    }

    return descendants;
  }
}

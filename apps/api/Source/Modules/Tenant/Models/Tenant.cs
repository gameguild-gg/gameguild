using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Tenant.Models;

/// <summary>
/// Represents a tenant in a multi-tenant system
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("Tenants")]
[Index(nameof(Name), IsUnique = true)]
public class Tenant : BaseEntity {
  private string _name = string.Empty;

  private string? _description;

  private bool _isActive = true;

  private string _slug = string.Empty;

  private ICollection<TenantPermission> _tenantPermissions = new List<TenantPermission>();

  /// <summary>
  /// Name of the tenant
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [MaxLength(500)]
  public string? Description {
    get => _description;
    set => _description = value;
  }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// Slug for the tenant (URL-friendly unique identifier)
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string Slug {
    get => _slug;
    set => _slug = value;
  }

  /// <summary>
  /// Navigation property to tenant permissions and user memberships
  /// </summary>
  public virtual ICollection<TenantPermission> TenantPermissions {
    get => _tenantPermissions;
    set => _tenantPermissions = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public Tenant() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial tenant data</param>
  public Tenant(object partial) : base(partial) { }

  /// <summary>
  /// Activate the tenant
  /// </summary>
  public void Activate() {
    IsActive = true;
    Touch();
  }

  /// <summary>
  /// Deactivate the tenant
  /// </summary>
  public void Deactivate() {
    IsActive = false;
    Touch();
  }
}

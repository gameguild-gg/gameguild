using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Models;


namespace GameGuild.Modules.User.Models;

[Table("Users")]
[Index(nameof(Email), IsUnique = true)]
public class User : BaseEntity {
  private string _name = string.Empty;

  private string _email = string.Empty;

  private bool _isActive = true;

  private decimal _balance = 0;

  private decimal _availableBalance = 0;

  private ICollection<Credential> _credentials = new List<Credential>();

  private ICollection<TenantPermission> _tenantPermissions = new List<TenantPermission>();

  private ICollection<ContentTypePermission> _contentTypePermissions = new List<ContentTypePermission>();

  [Required]
  [MaxLength(100)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  [Required]
  [EmailAddress]
  [MaxLength(255)]
  public string Email {
    get => _email;
    set => _email = value;
  }

  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// Total wallet balance including pending/frozen funds
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal Balance {
    get => _balance;
    set => _balance = value;
  }

  /// <summary>
  /// Available balance that can be spent (excludes frozen/pending funds)
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal AvailableBalance {
    get => _availableBalance;
    set => _availableBalance = value;
  }

  /// <summary>
  /// Navigation property to user credentials
  /// </summary>
  public virtual ICollection<Credential> Credentials {
    get => _credentials;
    set => _credentials = value;
  }

  /// <summary>
  /// Navigation property to tenant permissions and memberships
  /// </summary>
  public virtual ICollection<TenantPermission> TenantPermissions {
    get => _tenantPermissions;
    set => _tenantPermissions = value;
  }

  /// <summary>
  /// Navigation property to global content type permissions (Layer 2a of permission system)
  /// </summary>
  public virtual ICollection<ContentTypePermission> ContentTypePermissions {
    get => _contentTypePermissions;
    set => _contentTypePermissions = value;
  }

  /// <summary>
  /// <summary>
  /// Default constructor
  /// </summary>
  public User() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial user data</param>
  public User(object partial) : base(partial) { }
}

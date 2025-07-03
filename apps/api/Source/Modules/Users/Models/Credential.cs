using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.User.Models;

/// <summary>
/// Represents a user credential (password, API key, OAuth token, etc.)
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("Credentials")]
[Index(nameof(UserId), nameof(Type))]
public class Credential : BaseEntity, ITenantable {
  private Guid _userId;

  private User _user = null!;

  private Guid? _tenantId;

  private Tenant.Models.Tenant? _tenant;

  private string _type = string.Empty;

  private string _value = string.Empty;

  private string? _metadata;

  private DateTime? _expiresAt;

  private bool _isActive = true;

  private DateTime? _lastUsedAt;

  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Navigation property to the User entity
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual User User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Foreign key to the Tenant entity (optional - for tenant-specific credentials)
  /// </summary>
  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  /// <summary>
  /// Navigation property to the Tenant entity (hide base implementation)
  /// </summary>
  [ForeignKey(nameof(TenantId))]
  public new virtual Tenant.Models.Tenant? Tenant {
    get => _tenant;
    set => _tenant = value;
  }

  /// <summary>
  /// Indicates whether this credential is accessible across all tenants
  /// </summary>
  [NotMapped]
  public override bool IsGlobal {
    get => TenantId == null;
  }

  /// <summary>
  /// Type of credential (e.g., "password", "api_key", "oauth_token", "2fa_secret")
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Type {
    get => _type;
    set => _type = value;
  }

  /// <summary>
  /// The credential value (hashed password, encrypted token, etc.)
  /// </summary>
  [Required]
  [MaxLength(1000)]
  public string Value {
    get => _value;
    set => _value = value;
  }

  /// <summary>
  /// Additional metadata for the credential (JSON format)
  /// Can store salt, algorithm, expiration, etc.
  /// </summary>
  [MaxLength(2000)]
  public string? Metadata {
    get => _metadata;
    set => _metadata = value;
  }

  /// <summary>
  /// When this credential expires (optional)
  /// </summary>
  public DateTime? ExpiresAt {
    get => _expiresAt;
    set => _expiresAt = value;
  }

  /// <summary>
  /// Whether this credential is currently active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// When this credential was last used
  /// </summary>
  public DateTime? LastUsedAt {
    get => _lastUsedAt;
    set => _lastUsedAt = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public Credential() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial credential data</param>
  public Credential(object partial) : base(partial) { }

  /// <summary>
  /// Check if the credential is expired
  /// </summary>
  public bool IsExpired {
    get => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
  }

  /// <summary>
  /// Check if the credential is valid (active and not expired)
  /// </summary>
  public bool IsValid {
    get => IsActive && !IsExpired && !IsDeleted;
  }

  /// <summary>
  /// Mark the credential as used
  /// </summary>
  public void MarkAsUsed() {
    LastUsedAt = DateTime.UtcNow;
    Touch(); // Update the UpdatedAt timestamp
  }

  /// <summary>
  /// Deactivate the credential
  /// </summary>
  public void Deactivate() {
    IsActive = false;
    Touch();
  }

  /// <summary>
  /// Activate the credential
  /// </summary>
  public void Activate() {
    IsActive = true;
    Touch();
  }
}

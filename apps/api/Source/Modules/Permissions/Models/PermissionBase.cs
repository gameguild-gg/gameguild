namespace GameGuild;

/// <summary> Base class for all permission entities following DDD principles </summary>
public abstract class PermissionBase : EntityBase {
  protected PermissionBase() { }

  protected PermissionBase(Guid? userId, Guid? tenantId) {
    UserId = userId;
    TenantId = tenantId;
    // Initialize with no permissions - they will be added later
    PermissionFlags1 = 0;
    PermissionFlags2 = 0;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> User ID (null means it's a default permission) </summary>
  public Guid? UserId { get; protected set; }

  /// <summary> Tenant ID (null means it's a global permission) </summary>
  public Guid? TenantId { get; protected set; }

  /// <summary> Permission flags for bits 0-63 </summary>
  [Column(TypeName = "bigint")]
  public ulong PermissionFlags1 { get; protected set; }

  /// <summary> Permission flags for bits 64-127 </summary>
  [Column(TypeName = "bigint")]
  public ulong PermissionFlags2 { get; protected set; }

  /// <summary> When the permission was granted </summary>
  public override DateTime CreatedAt { get; set; }

  /// <summary> When the permission was last updated </summary>
  public override DateTime UpdatedAt { get; set; }

  /// <summary> Optional expiration date for temporary permissions </summary>
  public DateTime? ExpiresAt { get; protected set; }

  /// <summary> Check if this permission has expired </summary>
  public virtual bool IsExpired { get => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow; }

  /// <summary> Clear all permissions for this entity </summary>
  public virtual void ClearPermissions() {
    PermissionFlags1 = 0;
    PermissionFlags2 = 0;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> Set expiration date for this permission </summary>
  public virtual void SetExpiration(DateTime? expiresAt) {
    ExpiresAt = expiresAt;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> Check if this permission includes a specific permission type </summary>
  public virtual bool HasPermission(PermissionType permission) {
    if (IsExpired) return false;
    var bitPos = (int) permission;
    
    if (bitPos < 64) return (PermissionFlags1 & 1UL << bitPos) != 0;
    if (bitPos < 128) return (PermissionFlags2 & 1UL << bitPos - 64) != 0;
    
    return false;
  }

  /// <summary> Add a permission to this entity </summary>
  public virtual void AddPermission(PermissionType permission) {
    var bitPos = (int) permission;
    
    if (bitPos < 64) {
      var mask = 1UL << bitPos;
      PermissionFlags1 |= mask;
    }
    else if (bitPos < 128) {
      var mask = 1UL << bitPos - 64;
      PermissionFlags2 |= mask;
    }
    
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> Remove a permission from this entity </summary>
  public virtual void RemovePermission(PermissionType permission) {
    var bitPos = (int) permission;
    
    if (bitPos < 64) {
      var mask = 1UL << bitPos;
      PermissionFlags1 &= ~mask;
    }
    else if (bitPos < 128) {
      var mask = 1UL << bitPos - 64;
      PermissionFlags2 &= ~mask;
    }
    
    UpdatedAt = DateTime.UtcNow;
  }
}
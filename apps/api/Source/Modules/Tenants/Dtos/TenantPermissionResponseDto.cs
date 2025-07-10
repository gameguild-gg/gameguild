using GameGuild.Modules.Users;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant permission response
/// </summary>
public class TenantPermissionResponseDto {
  /// <summary>
  /// Unique identifier for the tenant permission
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Foreign key to the User entity (null for default permissions)
  /// </summary>
  public Guid? UserId { get; set; }

  /// <summary>
  /// Foreign key to the Tenant entity (null for global defaults)
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Whether this tenant permission is currently valid (not expired and not deleted)
  /// </summary>
  public bool IsValid { get; set; }

  /// <summary>
  /// Permission expiry date
  /// </summary>
  public DateTime? ExpiresAt { get; set; }

  /// <summary>
  /// Permission flags for bits 0-63
  /// </summary>
  public ulong PermissionFlags1 { get; set; }

  /// <summary>
  /// Permission flags for bits 64-127
  /// </summary>
  public ulong PermissionFlags2 { get; set; }

  /// <summary>
  /// Version number for optimistic concurrency control
  /// </summary>
  public int Version { get; set; }

  /// <summary>
  /// When the permission was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the permission was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// When the permission was soft deleted (null if not deleted)
  /// </summary>
  public DateTime? DeletedAt { get; set; }

  /// <summary>
  /// Whether the permission is soft deleted
  /// </summary>
  public bool IsDeleted {
    get => DeletedAt.HasValue;
  }

  /// <summary>
  /// Associated user information
  /// </summary>
  public UserResponseDto? User { get; set; }

  /// <summary>
  /// Associated tenant information
  /// </summary>
  public TenantResponseDto? Tenant { get; set; }
}

namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant response
/// </summary>
public class TenantResponseDto {
  /// <summary>
  /// Unique identifier for the tenant
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Name of the tenant
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of the tenant
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool IsActive { get; set; }

  /// <summary>
  /// Version number for optimistic concurrency control
  /// </summary>
  public int Version { get; set; }

  /// <summary>
  /// When the tenant was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the tenant was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// When the tenant was soft deleted (null if not deleted)
  /// </summary>
  public DateTime? DeletedAt { get; set; }

  /// <summary>
  /// Whether the tenant is soft deleted
  /// </summary>
  public bool IsDeleted {
    get => DeletedAt.HasValue;
  }

  /// <summary>
  /// Permissions and users in this tenant
  /// </summary>
  public ICollection<TenantPermissionResponseDto> TenantPermissions { get; set; } = new List<TenantPermissionResponseDto>();
}

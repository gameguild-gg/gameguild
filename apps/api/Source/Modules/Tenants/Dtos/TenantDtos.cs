using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.User.Dtos;


namespace GameGuild.Modules.Tenant.Dtos;

/// <summary>
/// DTO for creating a new tenant
/// </summary>
public class CreateTenantDto {
  private string _name = string.Empty;

  private string? _description;

  private bool _isActive = true;

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
}

/// <summary>
/// DTO for updating an existing tenant
/// </summary>
public class UpdateTenantDto {
  private string _name = string.Empty;

  private string? _description;

  private bool _isActive = true;

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
}

/// <summary>
/// DTO for tenant response
/// </summary>
public class TenantResponseDto {
  private Guid _id;

  private string _name = string.Empty;

  private string? _description;

  private bool _isActive;

  private int _version;

  private DateTime _createdAt;

  private DateTime _updatedAt;

  private DateTime? _deletedAt;

  private ICollection<TenantPermissionResponseDto> _tenantPermissions = new List<TenantPermissionResponseDto>();

  /// <summary>
  /// Unique identifier for the tenant
  /// </summary>
  public Guid Id {
    get => _id;
    set => _id = value;
  }

  /// <summary>
  /// Name of the tenant
  /// </summary>
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Description of the tenant
  /// </summary>
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
  /// Version number for optimistic concurrency control
  /// </summary>
  public int Version {
    get => _version;
    set => _version = value;
  }

  /// <summary>
  /// When the tenant was created
  /// </summary>
  public DateTime CreatedAt {
    get => _createdAt;
    set => _createdAt = value;
  }

  /// <summary>
  /// When the tenant was last updated
  /// </summary>
  public DateTime UpdatedAt {
    get => _updatedAt;
    set => _updatedAt = value;
  }

  /// <summary>
  /// When the tenant was soft deleted (null if not deleted)
  /// </summary>
  public DateTime? DeletedAt {
    get => _deletedAt;
    set => _deletedAt = value;
  }

  /// <summary>
  /// Whether the tenant is soft deleted
  /// </summary>
  public bool IsDeleted {
    get => DeletedAt.HasValue;
  }

  /// <summary>
  /// Permissions and users in this tenant
  /// </summary>
  public ICollection<TenantPermissionResponseDto> TenantPermissions {
    get => _tenantPermissions;
    set => _tenantPermissions = value;
  }
}

/// <summary>
/// DTO for tenant permission response
/// </summary>
public class TenantPermissionResponseDto {
  private Guid _id;

  private Guid? _userId;

  private Guid? _tenantId;

  private bool _isValid;

  private DateTime? _expiresAt;

  private ulong _permissionFlags1;

  private ulong _permissionFlags2;

  private int _version;

  private DateTime _createdAt;

  private DateTime _updatedAt;

  private DateTime? _deletedAt;

  private UserResponseDto? _user;

  private TenantResponseDto? _tenant;

  /// <summary>
  /// Unique identifier for the tenant permission
  /// </summary>
  public Guid Id {
    get => _id;
    set => _id = value;
  }

  /// <summary>
  /// Foreign key to the User entity (null for default permissions)
  /// </summary>
  public Guid? UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Foreign key to the Tenant entity (null for global defaults)
  /// </summary>
  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  /// <summary>
  /// Whether this tenant permission is currently valid (not expired and not deleted)
  /// </summary>
  public bool IsValid {
    get => _isValid;
    set => _isValid = value;
  }

  /// <summary>
  /// Permission expiry date
  /// </summary>
  public DateTime? ExpiresAt {
    get => _expiresAt;
    set => _expiresAt = value;
  }

  /// <summary>
  /// Permission flags for bits 0-63
  /// </summary>
  public ulong PermissionFlags1 {
    get => _permissionFlags1;
    set => _permissionFlags1 = value;
  }

  /// <summary>
  /// Permission flags for bits 64-127
  /// </summary>
  public ulong PermissionFlags2 {
    get => _permissionFlags2;
    set => _permissionFlags2 = value;
  }

  /// <summary>
  /// Version number for optimistic concurrency control
  /// </summary>
  public int Version {
    get => _version;
    set => _version = value;
  }

  /// <summary>
  /// When the permission was created
  /// </summary>
  public DateTime CreatedAt {
    get => _createdAt;
    set => _createdAt = value;
  }

  /// <summary>
  /// When the permission was last updated
  /// </summary>
  public DateTime UpdatedAt {
    get => _updatedAt;
    set => _updatedAt = value;
  }

  /// <summary>
  /// When the permission was soft deleted (null if not deleted)
  /// </summary>
  public DateTime? DeletedAt {
    get => _deletedAt;
    set => _deletedAt = value;
  }

  /// <summary>
  /// Whether the permission is soft deleted
  /// </summary>
  public bool IsDeleted {
    get => DeletedAt.HasValue;
  }

  /// <summary>
  /// Associated user information
  /// </summary>
  public UserResponseDto? User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Associated tenant information
  /// </summary>
  public TenantResponseDto? Tenant {
    get => _tenant;
    set => _tenant = value;
  }
}

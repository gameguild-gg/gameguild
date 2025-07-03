using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.User.Dtos;

/// <summary>
/// DTO for creating a new credential
/// </summary>
public class CreateCredentialDto {
  private Guid _userId;

  private string _type = string.Empty;

  private string _value = string.Empty;

  private string? _metadata;

  private DateTime? _expiresAt;

  private bool _isActive = true;

  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
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
}

/// <summary>
/// DTO for updating an existing credential
/// </summary>
public class UpdateCredentialDto {
  private string _type = string.Empty;

  private string _value = string.Empty;

  private string? _metadata;

  private DateTime? _expiresAt;

  private bool _isActive = true;

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
}

/// <summary>
/// DTO for credential response
/// </summary>
public class CredentialResponseDto {
  private Guid _id;

  private Guid _userId;

  private string _type = string.Empty;

  private string _value = string.Empty;

  private string? _metadata;

  private DateTime? _expiresAt;

  private bool _isActive;

  private DateTime? _lastUsedAt;

  private int _version;

  private DateTime _createdAt;

  private DateTime _updatedAt;

  private DateTime? _deletedAt;

  private UserResponseDto? _user;

  /// <summary>
  /// Unique identifier for the credential
  /// </summary>
  public Guid Id {
    get => _id;
    set => _id = value;
  }

  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Type of credential
  /// </summary>
  public string Type {
    get => _type;
    set => _type = value;
  }

  /// <summary>
  /// The credential value (should be masked/redacted in responses)
  /// </summary>
  public string Value {
    get => _value;
    set => _value = value;
  }

  /// <summary>
  /// Additional metadata for the credential
  /// </summary>
  public string? Metadata {
    get => _metadata;
    set => _metadata = value;
  }

  /// <summary>
  /// When this credential expires
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
  /// Version number for optimistic concurrency control
  /// </summary>
  public int Version {
    get => _version;
    set => _version = value;
  }

  /// <summary>
  /// When the credential was created
  /// </summary>
  public DateTime CreatedAt {
    get => _createdAt;
    set => _createdAt = value;
  }

  /// <summary>
  /// When the credential was last updated
  /// </summary>
  public DateTime UpdatedAt {
    get => _updatedAt;
    set => _updatedAt = value;
  }

  /// <summary>
  /// When the credential was soft deleted (null if not deleted)
  /// </summary>
  public DateTime? DeletedAt {
    get => _deletedAt;
    set => _deletedAt = value;
  }

  /// <summary>
  /// Whether the credential is soft deleted
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
}

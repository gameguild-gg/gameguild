using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.User.GraphQL;

/// <summary>
/// Input type for creating a new credential
/// </summary>
public class CreateCredentialInput {
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
/// Input type for updating an existing credential
/// </summary>
public class UpdateCredentialInput {
  private Guid _id;

  private string _type = string.Empty;

  private string _value = string.Empty;

  private string? _metadata;

  private DateTime? _expiresAt;

  private bool _isActive = true;

  /// <summary>
  /// Credential ID to update
  /// </summary>
  [Required]
  public Guid Id {
    get => _id;
    set => _id = value;
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

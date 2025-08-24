namespace GameGuild.Modules.Credentials;

/// <summary>
/// Input type for updating an existing credential
/// </summary>
public class UpdateCredentialInput {
  /// <summary>
  /// Credential ID to update
  /// </summary>
  [Required]
  public Guid Id { get; set; }

  /// <summary>
  /// Type of credential (e.g., "password", "api_key", "oauth_token", "2fa_secret")
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// The credential value (hashed password, encrypted token, etc.)
  /// </summary>
  [Required]
  [MaxLength(1000)]
  public string Value { get; set; } = string.Empty;

  /// <summary>
  /// Additional metadata for the credential (JSON format)
  /// </summary>
  [MaxLength(2000)]
  public string? Metadata { get; set; }

  /// <summary>
  /// When this credential expires (optional)
  /// </summary>
  public DateTime? ExpiresAt { get; set; }

  /// <summary>
  /// Whether this credential is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;
}

using MediatR;


namespace GameGuild.Modules.Credentials.Commands;

/// <summary>
/// Command to update an existing credential using CQRS pattern
/// </summary>
public class UpdateCredentialCommand : IRequest<Credential> {
  /// <summary>
  /// Credential ID to update
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Type of credential (e.g., "password", "api_key", "oauth_token", "2fa_secret")
  /// </summary>
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// The credential value (hashed password, encrypted token, etc.)
  /// </summary>
  public string Value { get; set; } = string.Empty;

  /// <summary>
  /// Additional metadata for the credential (JSON format)
  /// </summary>
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

using MediatR;


namespace GameGuild.Modules.Credentials.Notifications;

/// <summary>
/// Notification published when a credential is created
/// </summary>
public class CredentialCreatedNotification : INotification {
  /// <summary>
  /// The created credential ID
  /// </summary>
  public Guid CredentialId { get; set; }

  /// <summary>
  /// The user ID the credential belongs to
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// The type of credential
  /// </summary>
  public string Type { get; set; } = string.Empty;
}

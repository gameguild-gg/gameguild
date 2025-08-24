namespace GameGuild.Modules.Credentials.Commands;

/// <summary>
/// Command to delete a credential using CQRS pattern
/// </summary>
public class DeleteCredentialCommand : IRequest<bool> {
  /// <summary>
  /// Credential ID to delete
  /// </summary>
  public Guid Id { get; set; }
}

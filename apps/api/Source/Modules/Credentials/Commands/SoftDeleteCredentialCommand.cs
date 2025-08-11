using MediatR;


namespace GameGuild.Modules.Credentials.Commands;

/// <summary>
/// Command to soft delete a credential using CQRS pattern
/// </summary>
public class SoftDeleteCredentialCommand : IRequest<bool> {
  /// <summary>
  /// Credential ID to soft delete
  /// </summary>
  public Guid Id { get; set; }

  public SoftDeleteCredentialCommand(Guid id) { Id = id; }
}

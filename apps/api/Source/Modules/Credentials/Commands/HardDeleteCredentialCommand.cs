using GameGuild.CQRS;


namespace GameGuild.Modules.Credentials.Commands;

/// <summary> Command to hard delete a credential using CQRS pattern </summary>
public class HardDeleteCredentialCommand : IRequest<bool> {
  public HardDeleteCredentialCommand(Guid id) { Id = id; }

  /// <summary> Credential ID to permanently delete </summary>
  public Guid Id { get; set; }
}

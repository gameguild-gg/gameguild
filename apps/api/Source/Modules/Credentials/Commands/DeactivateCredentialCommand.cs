using GameGuild.CQRS;


namespace GameGuild.Modules.Credentials.Commands;

/// <summary> Command to deactivate a credential using CQRS pattern </summary>
public class DeactivateCredentialCommand : IRequest<bool> {
  public DeactivateCredentialCommand(Guid id) { Id = id; }

  /// <summary> Credential ID to deactivate </summary>
  public Guid Id { get; set; }
}

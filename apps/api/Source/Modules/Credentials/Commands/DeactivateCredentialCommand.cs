using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Command to deactivate a credential using CQRS pattern </summary>
public class DeactivateCredentialCommand(Guid id) : IRequest<bool>
{
    /// <summary> Credential ID to deactivate </summary>
    public Guid Id { get; set; } = id;
}

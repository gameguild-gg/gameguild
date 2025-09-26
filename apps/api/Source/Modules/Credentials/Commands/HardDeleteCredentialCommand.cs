using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Command to hard delete a credential using CQRS pattern </summary>
public class HardDeleteCredentialCommand(Guid id) : IRequest<bool>
{
    /// <summary> Credential ID to permanently delete </summary>
    public Guid Id { get; set; } = id;
}

using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Command to restore a soft-deleted credential using CQRS pattern </summary>
public class RestoreCredentialCommand(Guid id) : IRequest<bool>
{
    /// <summary> Credential ID to restore </summary>
    public Guid Id { get; set; } = id;
}

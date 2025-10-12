using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Event raised when a credential is deleted </summary>
public sealed class CredentialDeletedEvent(Guid credentialId, bool isSoftDelete) : DomainEventBase(credentialId, nameof(Credential))
{
    public Guid CredentialId { get; } = credentialId;

    public bool IsSoftDelete { get; } = isSoftDelete;
}

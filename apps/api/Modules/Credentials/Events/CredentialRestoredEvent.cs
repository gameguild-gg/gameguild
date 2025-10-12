using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Event raised when a credential is restored </summary>
public sealed class CredentialRestoredEvent(Guid credentialId) : DomainEventBase(credentialId, nameof(Credential))
{
    public Guid CredentialId { get; } = credentialId;
}

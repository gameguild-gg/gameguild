using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Event raised when a credential is deactivated </summary>
public sealed class CredentialDeactivatedEvent(Guid credentialId) : DomainEventBase(credentialId, nameof(Credential))
{
    public Guid CredentialId { get; } = credentialId;
}

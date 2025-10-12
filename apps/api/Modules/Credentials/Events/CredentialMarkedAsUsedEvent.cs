using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Event raised when a credential is marked as used </summary>
public sealed class CredentialMarkedAsUsedEvent(Guid credentialId, DateTime usedAt) : DomainEventBase(credentialId, nameof(Credential))
{
    public Guid CredentialId { get; } = credentialId;

    public DateTime UsedAt { get; } = usedAt;
}

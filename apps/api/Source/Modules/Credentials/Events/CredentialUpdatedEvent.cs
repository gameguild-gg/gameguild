using System.Collections.Generic;
using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Event raised when a credential is updated </summary>
public sealed class CredentialUpdatedEvent(Guid credentialId, Dictionary<string, object> changes) : DomainEventBase(credentialId, nameof(Credential))
{
    public Guid CredentialId { get; } = credentialId;

    public Dictionary<string, object> Changes { get; } = changes;
}

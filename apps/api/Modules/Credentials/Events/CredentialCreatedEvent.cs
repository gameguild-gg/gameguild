using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Event raised when a credential is created </summary>
public sealed class CredentialCreatedEvent(Guid credentialId, Guid userId, string type, DateTime createdAt) : DomainEventBase(credentialId, nameof(Credential))
{
    public Guid CredentialId { get; } = credentialId;

    public Guid UserId { get; } = userId;

    public string Type { get; } = type;

    public DateTime CreatedAt { get; } = createdAt;
}

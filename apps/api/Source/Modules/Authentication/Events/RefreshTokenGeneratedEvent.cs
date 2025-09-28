using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a refresh token is generated </summary>
public sealed class RefreshTokenGeneratedEvent(Guid userId, Guid tokenId, DateTime expiresAt, DateTime generatedAt) : DomainEventBase(userId, nameof(RefreshToken))
{
    public Guid UserId { get; } = userId;

    public Guid TokenId { get; } = tokenId;

    public DateTime ExpiresAt { get; } = expiresAt;

    public DateTime GeneratedAt { get; } = generatedAt;
}
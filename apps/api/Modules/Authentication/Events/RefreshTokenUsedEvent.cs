using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a refresh token is used </summary>
public sealed class RefreshTokenUsedEvent(Guid userId, Guid tokenId, DateTime usedAt) : DomainEventBase(userId, nameof(RefreshToken))
{
    public Guid UserId { get; } = userId;

    public Guid TokenId { get; } = tokenId;

    public DateTime UsedAt { get; } = usedAt;
}

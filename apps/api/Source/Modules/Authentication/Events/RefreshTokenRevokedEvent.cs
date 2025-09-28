using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a refresh token is revoked </summary>
public sealed class RefreshTokenRevokedEvent(Guid userId, Guid tokenId, string reason, DateTime revokedAt) : DomainEventBase(userId, nameof(RefreshToken))
{
    public Guid UserId { get; } = userId;

    public Guid TokenId { get; } = tokenId;

    public string Reason { get; } = reason;

    public DateTime RevokedAt { get; } = revokedAt;
}
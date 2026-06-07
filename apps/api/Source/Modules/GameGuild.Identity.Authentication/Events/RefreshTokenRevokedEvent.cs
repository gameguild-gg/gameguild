using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a refresh token is revoked
/// </summary>
public abstract record RefreshTokenRevokedEvent(string TokenId, Guid UserId, string Reason, DateTime RevokedAt) : INotification;

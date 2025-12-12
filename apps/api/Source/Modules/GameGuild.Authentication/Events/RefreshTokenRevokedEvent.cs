using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when a refresh token is revoked
/// </summary>
public abstract record RefreshTokenRevokedEvent(string TokenId, Guid UserId, string Reason, DateTime RevokedAt) : INotification;

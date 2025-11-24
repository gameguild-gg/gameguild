using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when a refresh token is generated
/// </summary>
public abstract record RefreshTokenGeneratedEvent(string TokenId, Guid UserId, DateTime ExpiresAt) : INotification;

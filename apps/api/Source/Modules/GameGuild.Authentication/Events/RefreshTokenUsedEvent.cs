using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when a refresh token is used
/// </summary>
public abstract record RefreshTokenUsedEvent(string TokenId, Guid UserId, DateTime UsedAt) : INotification;

using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a refresh token is used
/// </summary>
public record RefreshTokenUsedEvent(string TokenId, Guid UserId, DateTime UsedAt) : INotification;

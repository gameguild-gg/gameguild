using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a refresh token is generated
/// </summary>
public record RefreshTokenGeneratedEvent(string TokenId, Guid UserId, DateTime ExpiresAt) : INotification;

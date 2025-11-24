using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when a token is revoked
/// </summary>
public record TokenRevokedEvent(Guid UserId, string TokenId, string? IpAddress, DateTime Timestamp) : INotification;

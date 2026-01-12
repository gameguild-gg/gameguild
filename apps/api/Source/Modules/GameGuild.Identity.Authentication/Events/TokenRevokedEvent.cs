using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a token is revoked
/// </summary>
public record TokenRevokedEvent(Guid UserId, string TokenId, string? IpAddress, DateTime Timestamp) : INotification;

using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a refresh token is used
/// </summary>
public sealed record TokenRefreshedEvent(Guid UserId, string? IpAddress, DateTime Timestamp) : INotification;

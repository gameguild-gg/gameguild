using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when a refresh token is used
/// </summary>
public record TokenRefreshedEvent(Guid UserId, string? IpAddress, DateTime Timestamp) : INotification;

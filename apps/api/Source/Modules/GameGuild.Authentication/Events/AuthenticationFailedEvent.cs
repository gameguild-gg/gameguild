using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when authentication fails
/// </summary>
public abstract record AuthenticationFailedEvent(string Identifier, string Reason, string? IpAddress, string? UserAgent, DateTime Timestamp) : INotification;

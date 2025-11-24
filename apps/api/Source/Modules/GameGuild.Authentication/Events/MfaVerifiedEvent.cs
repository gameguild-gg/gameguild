using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when MFA is verified successfully
/// </summary>
public record MfaVerifiedEvent(Guid UserId, string Method, string? IpAddress, DateTime Timestamp) : INotification;

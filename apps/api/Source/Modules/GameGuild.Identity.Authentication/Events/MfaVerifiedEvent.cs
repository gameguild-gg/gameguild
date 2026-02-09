using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when MFA is verified successfully
/// </summary>
public sealed record MfaVerifiedEvent(Guid UserId, string Method, string? IpAddress, DateTime Timestamp) : INotification;

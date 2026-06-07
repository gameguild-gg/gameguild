using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when MFA verification fails
/// </summary>
public abstract record MfaVerificationFailedEvent(Guid UserId, string Method, string Reason, DateTime AttemptedAt) : INotification;

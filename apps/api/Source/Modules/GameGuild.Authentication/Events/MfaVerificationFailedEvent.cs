using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when MFA verification fails
/// </summary>
public abstract record MfaVerificationFailedEvent(Guid UserId, string Method, string Reason, DateTime AttemptedAt) : INotification;

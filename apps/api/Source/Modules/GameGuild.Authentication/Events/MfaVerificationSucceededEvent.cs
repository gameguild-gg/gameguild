using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when MFA verification succeeds
/// </summary>
public abstract record MfaVerificationSucceededEvent(Guid UserId, string Method, DateTime VerifiedAt) : INotification;

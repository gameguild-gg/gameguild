using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when MFA verification succeeds
/// </summary>
public abstract record MfaVerificationSucceededEvent(Guid UserId, string Method, DateTime VerifiedAt) : INotification;

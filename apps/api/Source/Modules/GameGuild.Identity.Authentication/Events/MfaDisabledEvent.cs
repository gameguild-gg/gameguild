using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when MFA is disabled for a user
/// </summary>
public abstract record MfaDisabledEvent(Guid UserId, string Method, DateTime DisabledAt) : INotification;

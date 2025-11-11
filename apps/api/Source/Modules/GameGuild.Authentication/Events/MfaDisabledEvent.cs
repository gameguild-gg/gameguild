using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when MFA is disabled for a user
/// </summary>
public abstract record MfaDisabledEvent(Guid UserId, string Method, DateTime DisabledAt) : INotification;

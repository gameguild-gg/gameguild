using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when MFA is enabled for a user
/// </summary>
public abstract record MfaEnabledEvent(Guid UserId, string Method, DateTime EnabledAt) : INotification;

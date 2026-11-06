using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when MFA is enabled for a user
/// </summary>
public record MfaEnabledEvent(Guid UserId, string Method, DateTime EnabledAt) : INotification;

using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when MFA is enabled for a user </summary>
public sealed class MfaEnabledEvent(Guid userId, string method, DateTime enabledAt) : DomainEventBase(userId, nameof(UserMfaConfiguration))
{
    public Guid UserId { get; } = userId;

    public string Method { get; } = method;

    public DateTime EnabledAt { get; } = enabledAt;
}

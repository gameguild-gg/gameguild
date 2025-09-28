using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when MFA is disabled for a user </summary>
public sealed class MfaDisabledEvent(Guid userId, string method, DateTime disabledAt) : DomainEventBase(userId, nameof(UserMfaConfiguration))
{
    public Guid UserId { get; } = userId;

    public string Method { get; } = method;

    public DateTime DisabledAt { get; } = disabledAt;
}
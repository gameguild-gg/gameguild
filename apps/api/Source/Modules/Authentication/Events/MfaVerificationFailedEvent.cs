using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when MFA verification fails </summary>
public sealed class MfaVerificationFailedEvent(Guid userId, string method, string reason, DateTime attemptedAt) : DomainEventBase(userId, nameof(UserMfaConfiguration))
{
    public Guid UserId { get; } = userId;

    public string Method { get; } = method;

    public string Reason { get; } = reason;

    public DateTime AttemptedAt { get; } = attemptedAt;
}

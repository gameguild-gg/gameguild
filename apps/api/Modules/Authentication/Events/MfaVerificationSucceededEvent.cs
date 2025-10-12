using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when MFA verification succeeds </summary>
public sealed class MfaVerificationSucceededEvent(Guid userId, string method, DateTime verifiedAt) : DomainEventBase(userId, nameof(UserMfaConfiguration))
{
    public Guid UserId { get; } = userId;

    public string Method { get; } = method;

    public DateTime VerifiedAt { get; } = verifiedAt;
}

using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when an authentication attempt fails </summary>
public sealed class AuthenticationFailedEvent(string email, string reason, string? ipAddress, string? userAgent, DateTime attemptedAt) : DomainEventBase(Guid.Empty, nameof(AuthenticationAttempt))
{
    public string Email { get; } = email;

    public string Reason { get; } = reason;

    public string? IpAddress { get; } = ipAddress;

    public string? UserAgent { get; } = userAgent;

    public DateTime AttemptedAt { get; } = attemptedAt;
}
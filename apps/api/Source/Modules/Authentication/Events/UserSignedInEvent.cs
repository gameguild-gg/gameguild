using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a user successfully signs in </summary>
public sealed class UserSignedInEvent(Guid userId, string email, string signInMethod, string? ipAddress, string? userAgent, DateTime signedInAt) : DomainEventBase(userId, nameof(User))
{
    public Guid UserId { get; } = userId;

    public string Email { get; } = email;

    public string SignInMethod { get; } = signInMethod;

    public string? IpAddress { get; } = ipAddress;

    public string? UserAgent { get; } = userAgent;

    public DateTime SignedInAt { get; } = signedInAt;
}
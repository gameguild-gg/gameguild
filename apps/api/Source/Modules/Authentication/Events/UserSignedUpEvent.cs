using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a user signs up </summary>
public sealed class UserSignedUpEvent(Guid userId, string email, string signUpMethod, DateTime signedUpAt) : DomainEventBase(userId, nameof(User))
{
    public Guid UserId { get; } = userId;

    public string Email { get; } = email;

    public string SignUpMethod { get; } = signUpMethod;

    public DateTime SignedUpAt { get; } = signedUpAt;
}
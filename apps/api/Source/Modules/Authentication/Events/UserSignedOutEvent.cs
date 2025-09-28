using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a user signs out </summary>
public sealed class UserSignedOutEvent(Guid userId, string email, DateTime signedOutAt) : DomainEventBase(userId, nameof(User))
{
    public Guid UserId { get; } = userId;

    public string Email { get; } = email;

    public DateTime SignedOutAt { get; } = signedOutAt;
}
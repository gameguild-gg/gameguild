using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a user signs up
/// </summary>
public record UserSignedUpEvent(Guid UserId, string Email, string AuthMethod, DateTime Timestamp) : INotification;

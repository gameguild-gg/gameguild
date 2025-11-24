using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Event published when a user signs up
/// </summary>
public record UserSignedUpEvent(Guid UserId, string Email, string AuthMethod, DateTime Timestamp) : INotification;

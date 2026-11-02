using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Event published when a user successfully signs in
/// </summary>
public record UserSignedInEvent(Guid UserId, string Email, string AuthMethod, string? IpAddress, string? UserAgent, DateTime Timestamp) : INotification;

using GameGuild.CQRS;

namespace GameGuild.Authentication.Events;

/// <summary>
///     Notification for user sign-up with additional details
/// </summary>
public abstract class UserSignedUpNotification : INotification
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }
}

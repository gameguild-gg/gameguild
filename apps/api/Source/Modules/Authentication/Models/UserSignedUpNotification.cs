using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Notification for user sign-up events that triggers side effects like welcome emails, analytics, etc. </summary>
public class UserSignedUpNotification : INotification
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Username { get; set; }

    public Guid? TenantId { get; set; }

    public DateTime SignUpTime { get; set; }
}

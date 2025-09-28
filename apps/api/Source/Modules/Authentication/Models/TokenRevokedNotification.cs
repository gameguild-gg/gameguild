using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Notification for token revocation events </summary>
public class TokenRevokedNotification : INotification
{
    public string RefreshToken { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public DateTime RevokedAt { get; set; }
}
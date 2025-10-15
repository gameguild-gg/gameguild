namespace GameGuild.Modules.Authentication;

/// <summary>
/// Domain notification published when a user token is revoked
/// </summary>
public class TokenRevokedNotification : INotification {
  public string RefreshToken { get; set; } = string.Empty;

  public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

  public string? IpAddress { get; set; }

  public Guid? UserId { get; set; }
}

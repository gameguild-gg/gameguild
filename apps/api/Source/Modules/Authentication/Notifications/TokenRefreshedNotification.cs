using MediatR;

namespace GameGuild.Modules.Auth;

/// <summary>
/// Domain notification published when a user token is refreshed
/// </summary>
public class TokenRefreshedNotification : INotification
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
    public Guid? TenantId { get; set; }
    public string? IpAddress { get; set; }
}

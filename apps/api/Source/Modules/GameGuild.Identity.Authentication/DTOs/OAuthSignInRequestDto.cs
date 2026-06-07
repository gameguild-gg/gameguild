namespace GameGuild.Identity.Authentication;

/// <summary>
///     Extended OAuth sign-in request DTO with additional metadata
/// </summary>
public class OAuthSignInRequestDto
{
    public string Provider { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public string? DeviceFingerprint { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

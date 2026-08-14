using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Linked external login as returned by the external-logins list endpoint.
/// </summary>
public sealed record ExternalLoginDto
{
    public required string Provider { get; init; }

    public required DateTime CreatedAt { get; init; }
}

/// <summary>
///     Request to link the signed-in user's Google account via an ID token.
/// </summary>
public class LinkGoogleAccountRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}

/// <summary>
///     Request to start the Discord link flow.
/// </summary>
public class DiscordLinkAuthorizeRequest
{
    [Required]
    public string RedirectUri { get; set; } = string.Empty;
}

/// <summary>
///     Request to complete the Discord link flow.
/// </summary>
public class DiscordLinkCallbackRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string RedirectUri { get; set; } = string.Empty;
}

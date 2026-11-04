using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request DTO for local sign-up
/// </summary>
public class LocalSignUpRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Optional tenant ID to use for the sign-up. If not provided, a default tenant may be assigned
    /// </summary>
    public Guid? TenantId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    /// <summary>
    ///     Optional guest bearer token presented by an anonymous visitor registering
    ///     (issue #213). A valid guest session is upgraded into the issued authenticated
    ///     session (session continuity); an invalid token never blocks registration.
    /// </summary>
    public string? GuestAccessToken { get; set; }

    /// <summary>
    ///     Cloudflare Turnstile anti-bot token (issue #1150). Optional at the contract
    ///     level, but REQUIRED whenever the API has a Turnstile secret configured
    ///     (Turnstile:SecretKey / TURNSTILE_SECRET_KEY): without a secret the check is
    ///     disabled; with one, sign-up fails closed until Cloudflare verifies the token.
    ///     The Next.js layer's client-side check alone was bypassable by direct API calls.
    /// </summary>
    public string? TurnstileToken { get; set; }
}

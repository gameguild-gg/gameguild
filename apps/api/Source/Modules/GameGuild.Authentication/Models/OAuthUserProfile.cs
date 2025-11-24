namespace GameGuild.Authentication.Models;

/// <summary>
///     Represents user profile information obtained from OAuth provider.
/// </summary>
public class OAuthUserProfile
{
    /// <summary>
    ///     Provider's unique identifier for the user.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    ///     The OAuth provider name (Google, GitHub, etc.).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    ///     User's email address from the provider.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     Whether the email is verified by the provider.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    ///     User's display name from the provider.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     User's first/given name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    ///     User's last/family name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    ///     Username from the provider (if available).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     URL to user's profile picture.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    ///     User's locale/language preference.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    ///     Access token from the provider.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    ///     Refresh token from the provider (if available).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    ///     When the access token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    ///     Additional claims/data from the provider.
    /// </summary>
    public Dictionary<string, object>? AdditionalClaims { get; set; }
}

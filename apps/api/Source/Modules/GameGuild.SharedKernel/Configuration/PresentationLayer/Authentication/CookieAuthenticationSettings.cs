using Microsoft.AspNetCore.Http;

namespace GameGuild.Configuration.PresentationLayer.Authentication;

/// <summary>
///     Configuration for the optional cookie authentication scheme.
/// </summary>
public sealed class CookieAuthenticationSettings
{
    private const string AllowedCookieNamePunctuation = "!#$%&'*+-.^_`|~";

    public string SchemeName { get; set; } = "GameGuildCookie";

    public string Name { get; set; } = "__Host-GameGuild.Auth";

    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(8);

    public bool SlidingExpiration { get; set; }

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchemeName))
        {
            throw new InvalidOperationException("Cookie authentication scheme name must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(Name) ||
            Name.Any(character => !char.IsAsciiLetterOrDigit(character) && !AllowedCookieNamePunctuation.Contains(character)))
        {
            throw new InvalidOperationException("Cookie authentication name must be a valid cookie token.");
        }

        if (Expiration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Cookie authentication expiration must be greater than zero.");
        }

        if (!Enum.IsDefined(SameSite) || SameSite == SameSiteMode.Unspecified)
        {
            throw new InvalidOperationException("Cookie authentication SameSite mode must be None, Lax, or Strict.");
        }
    }
}

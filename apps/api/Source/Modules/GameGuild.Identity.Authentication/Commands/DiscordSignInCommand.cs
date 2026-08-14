using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to initiate Discord OAuth sign-in flow.
///     Uses IOAuthService to generate the authorization URL with a CSRF state parameter.
/// </summary>
public sealed record DiscordSignInCommand : ICommand<DiscordSignInResponse>
{
    /// <summary>
    ///     The redirect URI after Discord authentication.
    /// </summary>
    public required string RedirectUri { get; init; }
}

/// <summary>
///     Response for Discord sign-in initiation
/// </summary>
public sealed record DiscordSignInResponse
{
    /// <summary>
    ///     Discord OAuth authorization URL
    /// </summary>
    public required string AuthUrl { get; init; }

    /// <summary>
    ///     CSRF state parameter embedded in the authorization URL (also returned separately
    ///     so the caller can stash it in its state cookie)
    /// </summary>
    public required string State { get; init; }
}

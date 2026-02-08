using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to initiate GitHub OAuth sign-in flow.
///     Uses IOAuthService to generate the proper authorization URL with PKCE/state parameters.
/// </summary>
public sealed record GitHubSignInCommand : ICommand<GitHubSignInResponse>
{
    /// <summary>
    ///     The redirect URI after GitHub authentication.
    /// </summary>
    public required string RedirectUri { get; init; }
}

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for signing out the authenticated caller.
/// </summary>
public class SignOutRequest
{
    /// <summary>
    ///     When true, revokes every refresh token and terminates every session for the account
    ///     (logout everywhere) instead of only the caller's current access token.
    /// </summary>
    public bool LogoutEverywhere { get; set; }
}

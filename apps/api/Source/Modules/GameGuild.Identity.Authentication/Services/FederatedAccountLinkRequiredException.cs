namespace GameGuild.Identity.Authentication;

/// <summary>
///     Sign-in with a federated identity (OAuth/OIDC/SAML) whose email matches an existing
///     account that was provisioned by a DIFFERENT provider (issue #1121). The identity
///     resolves by provider + subject, not by email alone: the caller already checked the
///     (provider, providerKey) link and found none, and the email collision is with an
///     account that belongs to another external provider. Silently linking here would let
///     whoever controls an email at one provider inherit an account minted at another —
///     instead the sign-in stops with this challenge and the account can only be attached
///     through the explicit, authenticated linking flow
///     (<c>ExternalLoginsController</c> link-authorize/link-callback), where the owner
///     proves control of the existing account.
///     <para>
///         Derives from <see cref="UnauthorizedAccessException" /> so the existing sign-in
///         error mapping answers with the standard 401 ProblemDetails; the detail text and
///         the <see cref="Provider" /> / <see cref="ExistingProvider" /> properties carry the
///         machine-readable linking signal for clients.
///     </para>
/// </summary>
public sealed class FederatedAccountLinkRequiredException : UnauthorizedAccessException
{
    /// <summary>The provider key prefix the incoming identity presented (e.g. <c>oidc-google</c>).</summary>
    public string Provider { get; }

    /// <summary>The provider that already owns the email-matched account, when known.</summary>
    public string? ExistingProvider { get; }

    /// <summary>The normalized email both providers returned.</summary>
    public string Email { get; }

    public FederatedAccountLinkRequiredException(string provider, string? existingProvider, string email)
        : base(
            $"An account already exists for {email} through a different sign-in provider"
            + (string.IsNullOrWhiteSpace(existingProvider) ? string.Empty : $" ({existingProvider})")
            + $". Sign in with that provider and link your {provider} account from account settings to continue.")
    {
        Provider = provider;
        ExistingProvider = existingProvider;
        Email = email;
    }
}

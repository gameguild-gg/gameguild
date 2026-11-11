using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Thrown when the external identity is already linked to a different GameGuild user.
///     Callers surface this as 409 Conflict — it must never be resolved by re-linking
///     (UpsertAsync would silently reassign ownership of the row).
/// </summary>
public sealed class ExternalLoginConflictException(string message) : Exception(message);

/// <summary>
///     Thrown when unlinking would remove the user's only remaining sign-in method
///     (no password and no other external login). Callers surface this as 400.
/// </summary>
public sealed class LastSignInMethodException(string message) : Exception(message);

/// <summary>
///     Thrown when the requested provider is not linked to the current user.
///     Callers surface this as 404.
/// </summary>
public sealed class ExternalLoginNotFoundException(string message) : Exception(message);

/// <summary>
///     Thrown when a caller-supplied scope token is not a valid scope token
///     (empty, whitespace, or control characters). Callers surface this as 400.
/// </summary>
public sealed class InvalidOAuthScopeException(string message) : Exception(message);

/// <summary>
///     Thrown when the state echoed by an OAuth link callback does not match a state this
///     server issued to the calling user for that provider (issue #1181: the Discord link
///     callback never round-trip validated its state server-side). Callers surface this as 400.
/// </summary>
public sealed class InvalidOAuthLinkStateException(string message) : Exception(message);

/// <summary>
///     Links the authenticated user's Google identity, verified from a Google ID token.
/// </summary>
public sealed record LinkGoogleAccountCommand : ICommand
{
    public required Guid UserId { get; init; }

    public required string IdToken { get; init; }
}

/// <summary>
///     Starts the Discord link flow: returns the authorization URL plus the
///     per-request state embedded in it (also recorded server-side for the
///     callback's round-trip validation).
/// </summary>
public sealed record DiscordLinkAuthorizeCommand : ICommand<DiscordLinkAuthorizeResponse>
{
    public required Guid UserId { get; init; }

    public required string RedirectUri { get; init; }
}

/// <summary>
///     Completes the Discord link flow: exchanges the authorization code for the
///     Discord profile and links the provider key to the user.
/// </summary>
public sealed record LinkDiscordAccountCommand : ICommand
{
    public required Guid UserId { get; init; }

    public required string Code { get; init; }

    public required string State { get; init; }

    public required string RedirectUri { get; init; }
}

/// <summary>
///     Starts a provider-parameterized link flow (issue #1181: GitHub and Microsoft had no
///     link endpoints at all). Returns the authorization URL plus the per-request state,
///     which is also recorded server-side for the callback's round-trip validation.
/// </summary>
public sealed record ExternalLoginLinkAuthorizeCommand : ICommand<ExternalLoginLinkAuthorizeResponse>
{
    public required Guid UserId { get; init; }

    public required string Provider { get; init; }

    public required string RedirectUri { get; init; }
}

/// <summary>
///     Completes a provider-parameterized link flow: validates the echoed state against the
///     server-issued record, exchanges the authorization code for the profile, and links the
///     provider key to the user.
/// </summary>
public sealed record ExternalLoginLinkCallbackCommand : ICommand
{
    public required Guid UserId { get; init; }

    public required string Provider { get; init; }

    public required string Code { get; init; }

    public required string State { get; init; }

    public required string RedirectUri { get; init; }
}

/// <summary>
///     Response for the provider-parameterized link-authorize endpoint.
/// </summary>
public sealed record ExternalLoginLinkAuthorizeResponse
{
    /// <summary>Provider OAuth authorization URL (state embedded).</summary>
    public required string AuthUrl { get; init; }

    /// <summary>Per-request state parameter, recorded server-side and validated at the callback.</summary>
    public required string State { get; init; }
}

/// <summary>
///     Removes the external login link for the given provider.
/// </summary>
public sealed record UnlinkExternalLoginCommand : ICommand
{
    public required Guid UserId { get; init; }

    public required string Provider { get; init; }
}

/// <summary>
///     Revokes individual OAuth scope grants on a linked provider without unlinking it.
///     Idempotent: scope tokens that are not currently granted are ignored.
/// </summary>
public sealed record RevokeExternalLoginScopesCommand : ICommand<RevokeExternalLoginScopesResponse>
{
    public required Guid UserId { get; init; }

    public required string Provider { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }
}

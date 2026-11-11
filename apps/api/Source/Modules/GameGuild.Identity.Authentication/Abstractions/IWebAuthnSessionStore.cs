namespace GameGuild.Identity.Authentication;

/// <summary>
///     Pending WebAuthn challenge sessions shared by the registration and authentication
///     sub-services. Sessions are short-lived (five minutes) and keyed by ceremony owner:
///     registration by user, assertion by user when known. Guest (discoverable-credential)
///     assertion sessions are keyed by their challenge token (issue #1198) so concurrent
///     anonymous ceremonies no longer share — and overwrite — a single slot. Implementations
///     must be thread-safe.
/// </summary>
public interface IWebAuthnSessionStore
{
    /// <summary>Stores a registration ceremony session for the user and returns its session id.</summary>
    Task<string> StoreRegistrationSessionAsync(WebAuthnSession session, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores an assertion ceremony session for the user and returns its session id. Guest
    ///     sessions (no user id) are stored under their challenge token so concurrent guest
    ///     ceremonies stay isolated (issue #1198).
    /// </summary>
    Task<string> StoreAssertionSessionAsync(WebAuthnSession session, CancellationToken cancellationToken = default);

    /// <summary>Returns the pending registration session for the user, or null when none/expired.</summary>
    Task<WebAuthnSession?> FindRegistrationSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the pending assertion session for the user, or null when none/expired.</summary>
    Task<WebAuthnSession?> FindAssertionSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the pending guest assertion session issued with the given challenge token
    ///     (the base64url <c>challenge</c> string of the assertion's client data), or null when
    ///     none/expired.
    /// </summary>
    Task<WebAuthnSession?> FindGuestAssertionSessionAsync(string challengeToken, CancellationToken cancellationToken = default);

    /// <summary>Removes the pending registration session for the user.</summary>
    Task RemoveRegistrationSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Removes the pending assertion session for the user.</summary>
    Task RemoveAssertionSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Removes the pending guest assertion session issued with the given challenge token.</summary>
    Task RemoveGuestAssertionSessionAsync(string challengeToken, CancellationToken cancellationToken = default);
}

/// <summary>A pending WebAuthn ceremony: the issued challenge and its options.</summary>
public sealed class WebAuthnSession
{
    public Guid? UserId { get; init; }

    public byte[] Challenge { get; init; } = [];

    public DateTime CreatedAt { get; init; } = SystemClock.UtcNow;

    public Fido2NetLib.CredentialCreateOptions? RegistrationOptions { get; init; }

    public Fido2NetLib.AssertionOptions? AssertionOptions { get; init; }
}

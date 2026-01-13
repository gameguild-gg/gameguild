namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository for managing WebAuthn/FIDO2 credentials.
/// </summary>
public interface IWebAuthnCredentialRepository
{
    /// <summary>
    ///     Get a credential by its unique ID.
    /// </summary>
    Task<UserWebAuthnCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a credential by its credential ID (from authenticator).
    /// </summary>
    Task<UserWebAuthnCredential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all credentials for a user.
    /// </summary>
    Task<List<UserWebAuthnCredential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all active credentials for a user.
    /// </summary>
    Task<List<UserWebAuthnCredential>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get credential IDs for a user (used during authentication).
    /// </summary>
    Task<List<string>> GetCredentialIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new credential.
    /// </summary>
    Task<UserWebAuthnCredential> CreateAsync(UserWebAuthnCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update a credential (e.g., signature counter).
    /// </summary>
    Task<UserWebAuthnCredential> UpdateAsync(UserWebAuthnCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a credential.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a user has any active WebAuthn credentials.
    /// </summary>
    Task<bool> HasActiveCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Count active credentials for a user.
    /// </summary>
    Task<int> CountActiveCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revoke a credential.
    /// </summary>
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update the signature counter and last used timestamp.
    /// </summary>
    Task UpdateSignatureCounterAsync(Guid id, uint newCounter, CancellationToken cancellationToken = default);
}

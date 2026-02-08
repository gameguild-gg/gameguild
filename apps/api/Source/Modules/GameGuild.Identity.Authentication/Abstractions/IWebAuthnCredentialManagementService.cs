namespace GameGuild.Identity.Authentication;

/// <summary>
///     Manages WebAuthn/FIDO2 credential CRUD operations and status queries.
/// </summary>
public interface IWebAuthnCredentialManagementService
{
    /// <summary>
    ///     Get all credentials for a user.
    /// </summary>
    Task<List<WebAuthnCredentialInfo>> GetUserCredentialsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a single credential by ID.
    /// </summary>
    Task<WebAuthnCredentialInfo?> GetCredentialByIdAsync(
        Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a credential exists for a user.
    /// </summary>
    Task<bool> CredentialExistsAsync(
        Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verify a credential is valid and can be used for authentication.
    /// </summary>
    Task<WebAuthnCredentialVerifyResult> VerifyCredentialAsync(
        Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a credential.
    /// </summary>
    Task<bool> DeleteCredentialAsync(
        Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update a credential's friendly name.
    /// </summary>
    Task<bool> UpdateCredentialNameAsync(
        Guid userId,
        Guid credentialId,
        string friendlyName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a user has WebAuthn enabled.
    /// </summary>
    Task<bool> IsWebAuthnEnabledAsync(Guid userId, CancellationToken cancellationToken = default);
}

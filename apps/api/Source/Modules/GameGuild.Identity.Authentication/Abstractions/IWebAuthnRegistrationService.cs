namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles WebAuthn/FIDO2 credential registration (attestation) flows.
/// </summary>
public interface IWebAuthnRegistrationService
{
    /// <summary>
    ///     Begin registration of a new WebAuthn credential.
    ///     Returns options to pass to the browser's navigator.credentials.create() API.
    /// </summary>
    Task<WebAuthnRegistrationOptionsResult> BeginRegistrationAsync(
        Guid userId,
        string userEmail,
        string displayName,
        WebAuthnAuthenticatorType? preferredType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Complete registration of a new WebAuthn credential.
    /// </summary>
    Task<WebAuthnRegistrationResult> CompleteRegistrationAsync(
        Guid userId,
        string attestationResponse,
        string? friendlyName = null,
        bool isPasswordless = false,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}

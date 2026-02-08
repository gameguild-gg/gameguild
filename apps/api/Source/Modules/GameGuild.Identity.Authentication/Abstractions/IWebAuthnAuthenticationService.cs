namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles WebAuthn/FIDO2 authentication (assertion) flows.
/// </summary>
public interface IWebAuthnAuthenticationService
{
    /// <summary>
    ///     Begin authentication with WebAuthn.
    ///     Returns options to pass to the browser's navigator.credentials.get() API.
    /// </summary>
    Task<WebAuthnAuthenticationOptionsResult> BeginAuthenticationAsync(
        string? userEmail = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Complete authentication with WebAuthn.
    /// </summary>
    Task<WebAuthnAuthenticationResult> CompleteAuthenticationAsync(
        string assertionResponse,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Controller for WebAuthn/FIDO2 passwordless authentication operations.
/// </summary>
[ApiController]
[Route("api/auth/webauthn")]
public class WebAuthnController(
    IWebAuthnService webAuthnService,
    ILogger<WebAuthnController> logger) : ControllerBase
{
    #region Registration Endpoints

    /// <summary>
    ///     Begin WebAuthn credential registration.
    /// </summary>
    /// <param name="request">Registration request with optional authenticator preferences.</param>
    /// <returns>Options to pass to navigator.credentials.create().</returns>
    [HttpPost("register/begin")]
    [Authorize]
    [ProducesResponseType(typeof(WebAuthnRegistrationOptionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WebAuthnRegistrationOptionsResult>> BeginRegistration(
        [FromBody] BeginWebAuthnRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await webAuthnService.BeginRegistrationAsync(
            userId.Value,
            request.Email,
            request.DisplayName,
            request.PreferredAuthenticatorType,
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    ///     Complete WebAuthn credential registration.
    /// </summary>
    /// <param name="request">The attestation response from the browser.</param>
    /// <returns>Result of the registration.</returns>
    [HttpPost("register/complete")]
    [Authorize]
    [ProducesResponseType(typeof(WebAuthnRegistrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WebAuthnRegistrationResult>> CompleteRegistration(
        [FromBody] CompleteWebAuthnRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await webAuthnService.CompleteRegistrationAsync(
            userId.Value,
            request.AttestationResponse,
            request.FriendlyName,
            request.IsPasswordless,
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion

    #region Authentication Endpoints

    /// <summary>
    ///     Begin WebAuthn authentication (passwordless login).
    /// </summary>
    /// <param name="request">Optional email to filter credentials.</param>
    /// <returns>Options to pass to navigator.credentials.get().</returns>
    [HttpPost("authenticate/begin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WebAuthnAuthenticationOptionsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<WebAuthnAuthenticationOptionsResult>> BeginAuthentication(
        [FromBody] BeginWebAuthnAuthenticationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await webAuthnService.BeginAuthenticationAsync(
            request?.Email,
            null,
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    ///     Complete WebAuthn authentication (passwordless login).
    /// </summary>
    /// <param name="request">The assertion response from the browser.</param>
    /// <returns>Authentication result with user info.</returns>
    [HttpPost("authenticate/complete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WebAuthnAuthenticationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WebAuthnAuthenticationResult>> CompleteAuthentication(
        [FromBody] CompleteWebAuthnAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await webAuthnService.CompleteAuthenticationAsync(
            request.AssertionResponse,
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        // Here you would typically generate JWT tokens for the authenticated user
        // This is a placeholder - integrate with your existing AuthService

        return Ok(result);
    }

    #endregion

    #region Credential Management Endpoints

    /// <summary>
    ///     Get all WebAuthn credentials for the current user.
    /// </summary>
    [HttpGet("credentials")]
    [Authorize]
    [ProducesResponseType(typeof(List<WebAuthnCredentialInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WebAuthnCredentialInfo>>> GetCredentials(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var credentials = await webAuthnService.GetUserCredentialsAsync(userId.Value, cancellationToken);
        return Ok(credentials);
    }

    /// <summary>
    ///     Delete a WebAuthn credential.
    /// </summary>
    /// <param name="credentialId">The credential ID to delete.</param>
    [HttpDelete("credentials/{credentialId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteCredential(
        Guid credentialId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await webAuthnService.DeleteCredentialAsync(userId.Value, credentialId, cancellationToken);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    ///     Update a WebAuthn credential's friendly name.
    /// </summary>
    /// <param name="credentialId">The credential ID to update.</param>
    /// <param name="request">The new friendly name.</param>
    [HttpPatch("credentials/{credentialId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateCredentialName(
        Guid credentialId,
        [FromBody] UpdateCredentialNameRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await webAuthnService.UpdateCredentialNameAsync(
            userId.Value, credentialId, request.FriendlyName, cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    ///     Check if current user has WebAuthn enabled.
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(typeof(WebAuthnStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WebAuthnStatusResponse>> GetWebAuthnStatus(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var isEnabled = await webAuthnService.IsWebAuthnEnabledAsync(userId.Value, cancellationToken);
        var credentials = await webAuthnService.GetUserCredentialsAsync(userId.Value, cancellationToken);

        return Ok(new WebAuthnStatusResponse
        {
            IsEnabled = isEnabled,
            CredentialCount = credentials.Count,
            HasPasswordlessCredential = credentials.Any(c => c.IsPasswordless),
            HasPlatformAuthenticator = credentials.Any(c => c.AuthenticatorType == WebAuthnAuthenticatorType.Platform),
            HasSecurityKey = credentials.Any(c => c.AuthenticatorType == WebAuthnAuthenticatorType.CrossPlatform)
        });
    }

    #endregion

    #region Helpers

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}

#region DTOs

/// <summary>
///     Request to begin WebAuthn registration.
/// </summary>
public class BeginWebAuthnRegistrationRequest
{
    /// <summary>
    ///     User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Display name for the credential.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     Preferred authenticator type (platform or cross-platform).
    /// </summary>
    public WebAuthnAuthenticatorType? PreferredAuthenticatorType { get; set; }
}

/// <summary>
///     Request to complete WebAuthn registration.
/// </summary>
public class CompleteWebAuthnRegistrationRequest
{
    /// <summary>
    ///     The JSON attestation response from navigator.credentials.create().
    /// </summary>
    public string AttestationResponse { get; set; } = string.Empty;

    /// <summary>
    ///     Optional friendly name for the credential.
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    ///     Whether this credential can be used for passwordless authentication.
    /// </summary>
    public bool IsPasswordless { get; set; }
}

/// <summary>
///     Request to begin WebAuthn authentication.
/// </summary>
public class BeginWebAuthnAuthenticationRequest
{
    /// <summary>
    ///     Optional email to filter credentials (for username-first flow).
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
///     Request to complete WebAuthn authentication.
/// </summary>
public class CompleteWebAuthnAuthenticationRequest
{
    /// <summary>
    ///     The JSON assertion response from navigator.credentials.get().
    /// </summary>
    public string AssertionResponse { get; set; } = string.Empty;
}

/// <summary>
///     Request to update a credential's friendly name.
/// </summary>
public class UpdateCredentialNameRequest
{
    /// <summary>
    ///     The new friendly name.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;
}

/// <summary>
///     Response for WebAuthn status check.
/// </summary>
public class WebAuthnStatusResponse
{
    /// <summary>
    ///     Whether WebAuthn is enabled for this user.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Number of registered credentials.
    /// </summary>
    public int CredentialCount { get; set; }

    /// <summary>
    ///     Whether user has at least one passwordless credential.
    /// </summary>
    public bool HasPasswordlessCredential { get; set; }

    /// <summary>
    ///     Whether user has a platform authenticator (Touch ID, Windows Hello).
    /// </summary>
    public bool HasPlatformAuthenticator { get; set; }

    /// <summary>
    ///     Whether user has a security key.
    /// </summary>
    public bool HasSecurityKey { get; set; }
}

#endregion

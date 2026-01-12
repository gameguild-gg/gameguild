using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Multi-Factor Authentication API Controller - RESTful API for MFA configuration and verification
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Tags("authentication/multi-factor")]
[Authorize]
public sealed class MfaController(IMfaService mfaService) : AuthControllerBase
{
    #region Configuration Operations - /v1/auth/mfa/configuration

    /// <summary>
    ///     Get current user's MFA configuration
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>MFA configuration details including enabled methods and status</returns>
    [HttpGet("v{version:apiVersion}/auth/mfa/configuration")]
    [EndpointSummary("Get MFA configuration")]
    [EndpointDescription("Retrieves the current user's multi-factor authentication configuration and enabled methods.")]
    [ProducesResponseType<MfaConfigurationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMfaConfiguration(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        MfaConfigurationResponse configuration = await mfaService.GetMfaConfigurationAsync(userId);

        return Ok(configuration);
    }

    #endregion

    #region TOTP Setup Operations - /v1/auth/mfa/setup/totp

    /// <summary>
    ///     Initiate TOTP MFA setup
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Setup response with secret key and QR code URI</returns>
    [HttpPost("v{version:apiVersion}/auth/mfa/setup/totp")]
    [EndpointSummary("Initiate TOTP setup")]
    [EndpointDescription("Initiates Time-based One-Time Password (TOTP) setup, returning a secret key and QR code URI for authenticator apps.")]
    [ProducesResponseType<MfaSetupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InitiateTotpSetup(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await mfaService.InitiateMfaSetupAsync(userId);

        if (!result.Success)
        {
            return BadRequest(new MfaErrorResponse { Error = string.IsNullOrEmpty(result.Message) ? "Failed to initiate MFA setup" : result.Message });
        }

        return Ok(new MfaSetupResponse
        {
            SecretKey = result.SecretKey,
            QrCodeUri = result.QrCodeUrl,
            BackupCodes = []
        });
    }

    /// <summary>
    ///     Complete TOTP MFA setup by verifying the code
    /// </summary>
    /// <param name="body">Verification code from authenticator app</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("v{version:apiVersion}/auth/mfa/setup/totp/complete")]
    [EndpointSummary("Complete TOTP setup")]
    [EndpointDescription("Completes TOTP setup by verifying a code from the user's authenticator app.")]
    [ProducesResponseType<MfaSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteTotpSetup([FromBody] CompleteMfaSetupRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var userId = GetCurrentUserId();
        var result = await mfaService.CompleteMfaSetupAsync(userId, body.Code);

        if (!result.Success)
        {
            return BadRequest(new MfaErrorResponse { Error = result.Message ?? "Failed to complete MFA setup" });
        }

        return Ok(new MfaSuccessResponse { Message = "MFA setup completed successfully" });
    }

    #endregion

    #region Verification Operations - /v1/auth/mfa/verify

    /// <summary>
    ///     Verify MFA code during authentication
    /// </summary>
    /// <param name="body">MFA verification request with code and method</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Verification result</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/mfa/verify")]
    [EndpointSummary("Verify MFA code")]
    [EndpointDescription("Verifies an MFA code during the authentication flow. Used after initial sign-in when MFA is required.")]
    [ProducesResponseType<MfaVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await mfaService.VerifyMfaAsync(body.UserId, body.Code, body.Method);

        if (!result.Success)
        {
            return BadRequest(new MfaErrorResponse { Error = result.Message ?? "MFA verification failed" });
        }

        return Ok(new MfaVerificationResponse { IsValid = result.Success });
    }

    #endregion

    #region Backup Codes Operations - /v1/auth/mfa/backup-codes

    /// <summary>
    ///     Generate new backup codes (invalidates existing ones)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New backup codes</returns>
    [HttpPost("v{version:apiVersion}/auth/mfa/backup-codes/regenerate")]
    [EndpointSummary("Regenerate backup codes")]
    [EndpointDescription("Generates a new set of backup codes, invalidating any previously generated codes.")]
    [ProducesResponseType<BackupCodesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegenerateBackupCodes(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var backupCodes = await mfaService.GenerateBackupCodesAsync(userId);

        return Ok(new BackupCodesResponse
        {
            Codes = [.. backupCodes],
            GeneratedAt = DateTime.UtcNow
        });
    }

    #endregion

    #region Disable Operations - /v1/auth/mfa/disable

    /// <summary>
    ///     Disable MFA for the current user
    /// </summary>
    /// <param name="body">Disable request with password confirmation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("v{version:apiVersion}/auth/mfa/disable")]
    [EndpointSummary("Disable MFA")]
    [EndpointDescription("Disables multi-factor authentication for the current user after password verification.")]
    [ProducesResponseType<MfaSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var userId = GetCurrentUserId();
        var result = await mfaService.DisableMfaAsync(userId, body.Password);

        if (!result)
        {
            return BadRequest(new MfaErrorResponse { Error = "Failed to disable MFA" });
        }

        return Ok(new MfaSuccessResponse { Message = "MFA disabled successfully" });
    }

    #endregion
}

/// <summary>
///     Success response for MFA operations
/// </summary>
public sealed record MfaSuccessResponse
{
    /// <summary>
    ///     Success message
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
///     Error response for MFA operations
/// </summary>
public sealed record MfaErrorResponse
{
    /// <summary>
    ///     Error message
    /// </summary>
    public required string Error { get; init; }
}

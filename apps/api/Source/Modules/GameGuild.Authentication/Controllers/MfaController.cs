using Asp.Versioning;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MfaSetupResult = GameGuild.Authentication.Models.Responses.MfaSetupResult;
using MfaVerificationResult = GameGuild.Authentication.Models.Responses.MfaVerificationResult;

namespace GameGuild.Authentication.Controllers;

/// <summary>
///     Controller for Multi-Factor Authentication operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/mfa")]
[Tags("Authentication")]
[Authorize]
public class MfaController(IMfaService mfaService, ILogger<MfaController> logger) : AuthControllerBase
{
    /// <summary>
    ///     Get current user's MFA configuration
    /// </summary>
    [HttpGet("configuration")]
    public async Task<ActionResult<MfaConfigurationResponse>> GetMfaConfiguration()
    {
        try
        {
            var userId = GetCurrentUserId();

            var configuration = await mfaService.GetMfaConfigurationAsync(userId);

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get MFA configuration for user {UserId}", GetCurrentUserId());

            return StatusCode(500, "Failed to retrieve MFA configuration");
        }
    }

    /// <summary>
    ///     Initiate TOTP MFA setup
    /// </summary>
    [HttpPost("setup/totp")]
    public async Task<ActionResult<MfaSetupResponse>> InitiateTotpSetup()
    {
        try
        {
            var userId = GetCurrentUserId();

            var result = await mfaService.InitiateMfaSetupAsync(userId);

            if (!result.Success) { return BadRequest(new { error = result.Message }); }

            return Ok(
                new MfaSetupResponse
                {
                    SecretKey = result.SecretKey!, QrCodeUri = result.QrCodeUrl!, BackupCodes = [] // Empty list for initial setup
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate TOTP setup for user {UserId}", GetCurrentUserId());

            return StatusCode(500, "Failed to initiate MFA setup");
        }
    }

    /// <summary>
    ///     Complete TOTP MFA setup by verifying the code
    /// </summary>
    [HttpPost("setup/totp/complete")]
    public async Task<ActionResult> CompleteTotpSetup([FromBody] CompleteMfaSetupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var result = await mfaService.CompleteMfaSetupAsync(userId, request.Code);

            if (!result.Success) { return BadRequest(new { error = result.Message }); }

            logger.LogInformation("User {UserId} successfully completed TOTP setup", userId);

            return Ok(new { message = "MFA setup completed successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete TOTP setup for user {UserId}", GetCurrentUserId());

            return StatusCode(500, "Failed to complete MFA setup");
        }
    }

    /// <summary>
    ///     Verify MFA code during authentication
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous] // This endpoint is used during login flow
    public async Task<ActionResult<MfaVerificationResponse>> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        try
        {
            var result = await mfaService.VerifyMfaAsync(request.UserId, request.Code, request.Method);

            if (!result.Success)
            {
                logger.LogWarning("Failed MFA verification for user {UserId} from IP {IpAddress}", request.UserId, HttpContext.Connection.RemoteIpAddress);

                return BadRequest(new { error = result.Message });
            }

            return Ok(new MfaVerificationResponse { IsValid = result.Success });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during MFA verification for user {UserId}", request.UserId);

            return StatusCode(500, "MFA verification failed");
        }
    }

    /// <summary>
    ///     Generate new backup codes (invalidates existing ones)
    /// </summary>
    [HttpPost("backup-codes/regenerate")]
    public async Task<ActionResult<BackupCodesResponse>> RegenerateBackupCodes()
    {
        try
        {
            var userId = GetCurrentUserId();

            var backupCodes = await mfaService.GenerateBackupCodesAsync(userId);

            logger.LogInformation("User {UserId} regenerated backup codes", userId);

            return Ok(new BackupCodesResponse { Codes = [.. backupCodes], GeneratedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to regenerate backup codes for user {UserId}", GetCurrentUserId());

            return StatusCode(500, "Failed to regenerate backup codes");
        }
    }

    /// <summary>
    ///     Disable MFA for the current user
    /// </summary>
    [HttpPost("disable")]
    public async Task<ActionResult> DisableMfa([FromBody] DisableMfaRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify password before disabling (DisableMfaRequest has Password property, not Code)
            // TODO: Verify password with user service when available

            var result = await mfaService.DisableMfaAsync(userId, request.Password);

            if (!result) { return BadRequest(new { error = "Failed to disable MFA" }); }

            logger.LogInformation("User {UserId} disabled MFA", userId);

            return Ok(new { message = "MFA disabled successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to disable MFA for user {UserId}", GetCurrentUserId());

            return StatusCode(500, "Failed to disable MFA");
        }
    }
}

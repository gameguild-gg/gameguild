using System.Security.Claims;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Authentication.Controllers;

/// <summary>
/// Controller for Multi-Factor Authentication operations
/// </summary>
[ApiController]
[Route("api/auth/mfa")]
[Authorize]
public class MfaController : BaseController {
    private readonly IMfaService _mfaService;
    private readonly ILogger<MfaController> _logger;

    public MfaController(IMfaService mfaService, ILogger<MfaController> logger) {
        _mfaService = mfaService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's MFA configuration
    /// </summary>
    [HttpGet("configuration")]
    public async Task<ActionResult<MfaConfigurationResponse>> GetMfaConfiguration() {
        // TODO: Implement when GetMfaConfigurationAsync is available in IMfaService
        var userId = GetCurrentUserId();
        if (userId == null) {
            return Unauthorized("User ID not found in token");
        }

        var isMfaEnabled = await _mfaService.IsMfaEnabledAsync(userId.Value);

        return Ok(new MfaConfigurationResponse {
            IsEnabled = isMfaEnabled,
            AvailableMethods = isMfaEnabled ? [MfaMethod.Totp] : [],
            BackupCodesRemaining = 0
        });
    }

    /// <summary>
    /// Initiate TOTP MFA setup
    /// </summary>
    [HttpPost("setup/totp")]
    public async Task<ActionResult<MfaSetupResponse>> InitiateTotpSetup() {
        try {
            var userId = GetCurrentUserId();
            if (userId == null) {
                return Unauthorized("User ID not found in token");
            }

            var result = await _mfaService.InitiateMfaSetupAsync(userId.Value);

            if (!result.IsSuccess) {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new MfaSetupResponse {
                SetupId = Guid.NewGuid().ToString(), // Temporary setup ID
                SecretKey = result.SecretKey!,
                QrCodeUrl = result.QrCodeData!,
                BackupCodes = new List<string>() // Empty list for initial setup
            });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to initiate TOTP setup for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to initiate MFA setup");
        }
    }

    /// <summary>
    /// Complete TOTP MFA setup by verifying the code
    /// </summary>
    [HttpPost("setup/totp/complete")]
    public async Task<ActionResult> CompleteTotpSetup([FromBody] CompleteMfaSetupRequest request) {
        try {
            var userId = GetCurrentUserId();
            if (userId == null) {
                return Unauthorized("User ID not found in token");
            }

            var result = await _mfaService.CompleteMfaSetupAsync(userId.Value, request.Code);

            if (!result.IsSuccess) {
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("User {UserId} successfully completed TOTP setup", userId);
            return Ok(new { message = "MFA setup completed successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to complete TOTP setup for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to complete MFA setup");
        }
    }

    /// <summary>
    /// Verify MFA code during authentication
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous] // This endpoint is used during login flow
    public async Task<ActionResult<MfaVerificationResponse>> VerifyMfa([FromBody] VerifyMfaRequest request) {
        try {
            var result = await _mfaService.VerifyMfaAsync(request.UserId, request.Code, request.Method);

            if (!result.IsSuccess) {
                _logger.LogWarning("Failed MFA verification for user {UserId} from IP {IpAddress}",
                  request.UserId, HttpContext.Connection.RemoteIpAddress);

                return BadRequest(new { error = result.Message });
            }

            return Ok(new MfaVerificationResponse {
                IsValid = result.IsSuccess,
                IsBackupCode = false, // MfaVerificationResult doesn't track this
                RemainingBackupCodes = null // MfaVerificationResult doesn't track this
            });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error during MFA verification for user {UserId}", request.UserId);
            return StatusCode(500, "MFA verification failed");
        }
    }

    /// <summary>
    /// Generate new backup codes (invalidates existing ones)
    /// </summary>
    [HttpPost("backup-codes/regenerate")]
    public async Task<ActionResult<BackupCodesResponse>> RegenerateBackupCodes() {
        try {
            var userId = GetCurrentUserId();
            if (userId == null) {
                return Unauthorized("User ID not found in token");
            }

            var backupCodes = await _mfaService.GenerateBackupCodesAsync(userId.Value);

            _logger.LogInformation("User {UserId} regenerated backup codes", userId);

            return Ok(new BackupCodesResponse {
                BackupCodes = [.. backupCodes]
            });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to regenerate backup codes for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to regenerate backup codes");
        }
    }

    /// <summary>
    /// Disable MFA for the current user
    /// </summary>
    [HttpPost("disable")]
    public async Task<ActionResult> DisableMfa([FromBody] DisableMfaRequest request) {
        try {
            var userId = GetCurrentUserId();
            if (userId == null) {
                return Unauthorized("User ID not found in token");
            }

            // Verify current MFA code before disabling
            var verificationResult = await _mfaService.VerifyMfaAsync(userId.Value, request.Code, MfaMethod.Totp);

            if (!verificationResult.IsSuccess) {
                return BadRequest(new { error = "Invalid MFA code" });
            }

            var result = await _mfaService.DisableMfaAsync(userId.Value, request.Code);

            if (!result) {
                return BadRequest(new { error = "Failed to disable MFA" });
            }

            _logger.LogInformation("User {UserId} disabled MFA", userId);
            return Ok(new { message = "MFA disabled successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to disable MFA for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to disable MFA");
        }
    }
}

// Request/Response DTOs

public class MfaConfigurationResponse {
    public bool IsEnabled { get; set; }
    public List<MfaMethod> AvailableMethods { get; set; } = [];
    public int BackupCodesRemaining { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class MfaSetupResponse {
    public string SetupId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = [];
}

public class CompleteMfaSetupRequest {
    public string SetupId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VerifyMfaRequest {
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public MfaMethod Method { get; set; } = MfaMethod.Totp;
}

public class MfaVerificationResponse {
    public bool IsValid { get; set; }
    public bool IsBackupCode { get; set; }
    public int? RemainingBackupCodes { get; set; }
}

public class BackupCodesResponse {
    public List<string> BackupCodes { get; set; } = [];
}

public class DisableMfaRequest {
    public string Code { get; set; } = string.Empty;
}

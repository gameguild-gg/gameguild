using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Controller for JWT key rotation management
/// </summary>
[ApiController]
[Route("api/auth/keys")]
[Authorize(Roles = "SystemAdministrator")]
public class KeyRotationController : ControllerBase
{
    private readonly IKeyRotationService _keyRotationService;
    private readonly ILogger<KeyRotationController> _logger;

    public KeyRotationController(
        IKeyRotationService keyRotationService,
        ILogger<KeyRotationController> logger)
    {
        _keyRotationService = keyRotationService;
        _logger = logger;
    }

    /// <summary>
    ///     Get the current active signing key info
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(JwtKeyInfoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtKeyInfoDto>> GetActiveKey(CancellationToken cancellationToken)
    {
        var key = await _keyRotationService.GetActiveSigningKeyAsync(cancellationToken);
        if (key == null)
            return NotFound(new { message = "No active signing key found" });

        return Ok(JwtKeyInfoDto.FromEntity(key));
    }

    /// <summary>
    ///     Get all valid keys for token validation
    /// </summary>
    [HttpGet("valid")]
    [ProducesResponseType(typeof(List<JwtKeyInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JwtKeyInfoDto>>> GetValidKeys(CancellationToken cancellationToken)
    {
        var keys = await _keyRotationService.GetValidationKeysAsync(cancellationToken);
        return Ok(keys.Select(JwtKeyInfoDto.FromEntity).ToList());
    }

    /// <summary>
    ///     Manually rotate to a new signing key
    /// </summary>
    [HttpPost("rotate")]
    [ProducesResponseType(typeof(JwtKeyInfoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtKeyInfoDto>> RotateKey(
        [FromBody] RotateKeyRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Manual key rotation requested by {User}. Reason: {Reason}",
            User.Identity?.Name, request.Reason);

        var newKey = await _keyRotationService.RotateKeyAsync(
            request.Reason ?? "manual-rotation",
            request.ValidityDays ?? 90,
            cancellationToken);

        return Ok(JwtKeyInfoDto.FromEntity(newKey));
    }

    /// <summary>
    ///     Clean up expired keys
    /// </summary>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(CleanupResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CleanupResult>> CleanupExpiredKeys(
        [FromBody] CleanupKeysRequest? request,
        CancellationToken cancellationToken)
    {
        var count = await _keyRotationService.CleanupExpiredKeysAsync(
            request?.RetentionDays ?? 30,
            cancellationToken);

        return Ok(new CleanupResult { DeletedCount = count });
    }
}

/// <summary>
///     DTO for JWT signing key information (without exposing key material)
/// </summary>
public record JwtKeyInfoDto
{
    public string KeyId { get; init; } = string.Empty;
    public string Algorithm { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RotatedAt { get; init; }
    public string? RotationReason { get; init; }
    public int KeyVersion { get; init; }

    public static JwtKeyInfoDto FromEntity(JwtSigningKey key)
    {
        return new JwtKeyInfoDto
        {
            KeyId = key.KeyId,
            Algorithm = key.Algorithm,
            IsActive = key.IsActive,
            ValidFrom = key.ValidFrom,
            ExpiresAt = key.ExpiresAt,
            RotatedAt = key.RotatedAt,
            RotationReason = key.RotationReason,
            KeyVersion = key.KeyVersion
        };
    }
}

/// <summary>
///     Request to manually rotate signing key
/// </summary>
public record RotateKeyRequest
{
    public string? Reason { get; init; }
    public int? ValidityDays { get; init; }
}

/// <summary>
///     Request to cleanup expired keys
/// </summary>
public record CleanupKeysRequest
{
    public int? RetentionDays { get; init; }
}

/// <summary>
///     Result of cleanup operation
/// </summary>
public record CleanupResult
{
    public int DeletedCount { get; init; }
}

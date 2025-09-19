using System.Security.Claims;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Authentication.Controllers;

/// <summary>
/// Controller for session and device management
/// </summary>
[ApiController]
[Route("api/auth/sessions")]
[Authorize]
public class SessionController : BaseController {
    private readonly ISessionManagementService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionManagementService sessionService, ILogger<SessionController> logger) {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's active sessions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SessionResponse>>> GetSessions() {
        try {
            var userId = GetCurrentUserId();
            var sessions = await _sessionService.GetUserSessionsAsync(userId, true);

            var response = sessions.Select(s => new SessionResponse {
                Id = s.Id,
                DeviceInfo = ParseDeviceInfo(s.DeviceInfo),
                Location = ParseLocation(s.Location),
                IpAddress = s.IpAddress,
                CreatedAt = s.CreatedAt,
                LastUsedAt = s.LastUsedAt,
                ExpiresAt = s.ExpiresAt,
                IsTrustedDevice = s.IsTrustedDevice,
                IsCurrent = IsCurrentSession(s.Id)
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get sessions for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to retrieve sessions");
        }
    }

    /// <summary>
    /// Get session security analysis
    /// </summary>
    [HttpGet("security-analysis")]
    public async Task<ActionResult<SessionSecurityAnalysis>> GetSecurityAnalysis() {
        try {
            var userId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var analysis = await _sessionService.AnalyzeSessionSecurityAsync(userId, ipAddress, userAgent);

            return Ok(analysis);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to analyze session security for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to analyze session security");
        }
    }

    /// <summary>
    /// Terminate a specific session
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> TerminateSession(Guid sessionId) {
        try {
            var userId = GetCurrentUserId();

            // Verify the session belongs to the user
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || session.UserId != userId) {
                return NotFound("Session not found");
            }

            var success = await _sessionService.TerminateSessionAsync(sessionId, SessionTerminationReason.UserRevoked);

            if (!success) {
                return BadRequest("Failed to terminate session");
            }

            _logger.LogInformation("User {UserId} terminated session {SessionId}", userId, sessionId);
            return Ok(new { message = "Session terminated successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to terminate session {SessionId} for user {UserId}", sessionId, GetCurrentUserId());
            return StatusCode(500, "Failed to terminate session");
        }
    }

    /// <summary>
    /// Terminate all sessions except the current one
    /// </summary>
    [HttpDelete("others")]
    public async Task<ActionResult> TerminateOtherSessions() {
        try {
            var userId = GetCurrentUserId();
            var currentSessionId = GetCurrentSessionId();

            var terminatedCount = await _sessionService.TerminateAllUserSessionsAsync(
              userId,
              SessionTerminationReason.UserRevoked,
              currentSessionId);

            _logger.LogInformation("User {UserId} terminated {Count} other sessions", userId, terminatedCount);

            return Ok(new {
                message = "Other sessions terminated successfully",
                terminatedCount
            });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to terminate other sessions for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to terminate other sessions");
        }
    }

    /// <summary>
    /// Terminate all sessions (including current)
    /// </summary>
    [HttpDelete("all")]
    public async Task<ActionResult> TerminateAllSessions() {
        try {
            var userId = GetCurrentUserId();

            var terminatedCount = await _sessionService.TerminateAllUserSessionsAsync(
              userId,
              SessionTerminationReason.UserRevoked);

            _logger.LogInformation("User {UserId} terminated all {Count} sessions", userId, terminatedCount);

            return Ok(new {
                message = "All sessions terminated successfully",
                terminatedCount
            });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to terminate all sessions for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to terminate all sessions");
        }
    }

    /// <summary>
    /// Get trusted devices for the current user
    /// </summary>
    [HttpGet("trusted-devices")]
    public async Task<ActionResult<List<TrustedDeviceResponse>>> GetTrustedDevices() {
        try {
            var userId = GetCurrentUserId();
            var devices = await _sessionService.GetTrustedDevicesAsync(userId);

            var response = devices.Select(d => new TrustedDeviceResponse {
                Id = d.Id,
                DeviceName = d.DeviceName,
                DeviceInfo = ParseDeviceInfo(d.DeviceInfo),
                TrustedAt = d.TrustedAt,
                LastUsedAt = d.LastUsedAt,
                ExpiresAt = d.ExpiresAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get trusted devices for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to retrieve trusted devices");
        }
    }

    /// <summary>
    /// Trust the current device
    /// </summary>
    [HttpPost("trust-device")]
    public async Task<ActionResult> TrustCurrentDevice([FromBody] TrustDeviceRequest request) {
        try {
            var userId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            // Generate device fingerprint
            var deviceFingerprint = GenerateDeviceFingerprint(ipAddress, userAgent);

            var success = await _sessionService.TrustDeviceAsync(userId, deviceFingerprint, request.DeviceName);

            if (!success) {
                return BadRequest("Failed to trust device");
            }

            _logger.LogInformation("User {UserId} trusted device {DeviceName}", userId, request.DeviceName);
            return Ok(new { message = "Device trusted successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to trust device for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to trust device");
        }
    }

    /// <summary>
    /// Revoke trust for a specific device
    /// </summary>
    [HttpDelete("trusted-devices/{deviceId}")]
    public async Task<ActionResult> RevokeTrustedDevice(Guid deviceId) {
        try {
            var userId = GetCurrentUserId();

            var success = await _sessionService.RevokeTrustedDeviceAsync(userId, deviceId);

            if (!success) {
                return NotFound("Trusted device not found");
            }

            _logger.LogInformation("User {UserId} revoked trusted device {DeviceId}", userId, deviceId);
            return Ok(new { message = "Device trust revoked successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to revoke trusted device {DeviceId} for user {UserId}", deviceId, GetCurrentUserId());
            return StatusCode(500, "Failed to revoke device trust");
        }
    }

    /// <summary>
    /// Refresh the current session
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshSession() {
        try {
            var currentSessionId = GetCurrentSessionId();

            if (currentSessionId == null) {
                return BadRequest("No active session found");
            }

            var success = await _sessionService.RefreshSessionAsync(currentSessionId.Value);

            if (!success) {
                return BadRequest("Failed to refresh session");
            }

            return Ok(new { message = "Session refreshed successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to refresh session for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Failed to refresh session");
        }
    }

    private Guid GetCurrentUserId() {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    private Guid? GetCurrentSessionId() {
        var sessionIdClaim = User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId)) {
            return null;
        }
        return sessionId;
    }

    private bool IsCurrentSession(Guid sessionId) {
        var currentSessionId = GetCurrentSessionId();
        return currentSessionId == sessionId;
    }

    private DeviceInfo? ParseDeviceInfo(string? deviceInfoJson) {
        if (string.IsNullOrEmpty(deviceInfoJson)) return null;

        try {
            return System.Text.Json.JsonSerializer.Deserialize<DeviceInfo>(deviceInfoJson);
        }
        catch {
            return null;
        }
    }

    private LocationInfo? ParseLocation(string? locationJson) {
        if (string.IsNullOrEmpty(locationJson)) return null;

        try {
            return System.Text.Json.JsonSerializer.Deserialize<LocationInfo>(locationJson);
        }
        catch {
            return null;
        }
    }

    private string GenerateDeviceFingerprint(string ipAddress, string userAgent) {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = $"{ipAddress}:{userAgent}";
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }
}

// Request/Response DTOs

public class SessionResponse {
    public Guid Id { get; set; }
    public DeviceInfo? DeviceInfo { get; set; }
    public LocationInfo? Location { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsTrustedDevice { get; set; }
    public bool IsCurrent { get; set; }
}

public class TrustedDeviceResponse {
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DeviceInfo? DeviceInfo { get; set; }
    public DateTime TrustedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class TrustDeviceRequest {
    public string DeviceName { get; set; } = string.Empty;
}

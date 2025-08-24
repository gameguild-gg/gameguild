using GameGuild.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Common.Controllers;

/// <summary>
/// Controller for Cloudflare Dynamic DNS service management and monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CloudflareController : ControllerBase {
  private readonly ILogger<CloudflareController> _logger;
  private readonly ICloudflareExternalIpService _cloudflareService;

  public CloudflareController(
    ILogger<CloudflareController> logger,
    ICloudflareExternalIpService cloudflareService) {
    _logger = logger;
    _cloudflareService = cloudflareService;
  }

  /// <summary>
  /// Gets the current status of the Cloudflare Dynamic DNS service.
  /// </summary>
  [HttpGet("status")]
  public ActionResult<CloudflareServiceStatus> GetStatus() {
    try {
      var status = new CloudflareServiceStatus {
        IsRunning = _cloudflareService.IsRunning,
        LastKnownIp = _cloudflareService.LastKnownIp,
        LastUpdate = _cloudflareService.LastUpdate
      };

      return Ok(status);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error retrieving Cloudflare service status");
      return StatusCode(500, "Internal server error");
    }
  }

  /// <summary>
  /// Manually triggers an IP check and DNS update.
  /// </summary>
  [HttpPost("update")]
  public async Task<ActionResult> TriggerUpdate(CancellationToken cancellationToken = default) {
    try {
      await _cloudflareService.UpdateExternalIpAsync(cancellationToken);
      _logger.LogInformation("Manual Cloudflare DNS update triggered");
      return Ok(new { message = "Update triggered successfully" });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error triggering manual Cloudflare DNS update");
      return StatusCode(500, "Internal server error");
    }
  }

  /// <summary>
  /// Gets the current external IP address.
  /// </summary>
  [HttpGet("external-ip")]
  public async Task<ActionResult<string>> GetExternalIp(CancellationToken cancellationToken = default) {
    try {
      var ip = await _cloudflareService.GetExternalIpAsync(cancellationToken);
      if (string.IsNullOrEmpty(ip)) {
        return NotFound("Unable to retrieve external IP");
      }

      return Ok(new { externalIp = ip });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error retrieving external IP");
      return StatusCode(500, "Internal server error");
    }
  }
}

/// <summary>
/// Status information for the Cloudflare Dynamic DNS service.
/// </summary>
public class CloudflareServiceStatus {
  public bool IsRunning { get; set; }
  public string? LastKnownIp { get; set; }
  public DateTime? LastUpdate { get; set; }
}

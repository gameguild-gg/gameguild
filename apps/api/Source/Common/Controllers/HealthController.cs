using GameGuild.Common.Services;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Common;

[ApiController]
[Route("[controller]")]
public class HealthController(ApplicationDbContext context, ICloudflareExternalIpService cloudflareService) : ControllerBase {
  /// <summary>
  /// Health check endpoint - publicly accessible for connectivity testing
  /// </summary>
  [HttpGet]
  [Public]
  public async Task<IActionResult> GetHealth() {
    try {
      // Check database connectivity
      await context.Database.CanConnectAsync();

      // Get Cloudflare Dynamic DNS status
      var dynamicDnsStatus = GetDynamicDnsStatus();

      return Ok(new {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        database = "connected",
        dynamicDns = dynamicDnsStatus
      });
    }
    catch (Exception ex) {
      return StatusCode(
        503,
        new {
          status = "unhealthy",
          timestamp = DateTime.UtcNow,
          environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
          database = "disconnected",
          error = ex.Message,
        }
      );
    }
  }

  /// <summary>
  /// Database health check
  /// </summary>
  [HttpGet("database")]
  public async Task<IActionResult> GetDatabaseHealth() {
    try {
      var canConnect = await context.Database.CanConnectAsync();
      var userCount = await context.Users.CountAsync();

      return Ok(new { status = "healthy", connected = canConnect, userCount, timestamp = DateTime.UtcNow });
    }
    catch (Exception ex) {
      return StatusCode(
        503,
        new { status = "unhealthy", connected = false, error = ex.Message, timestamp = DateTime.UtcNow }
      );
    }
  }

  /// <summary>
  /// Gets the current status of the Cloudflare Dynamic DNS service
  /// </summary>
  private object GetDynamicDnsStatus() {
    try {
      return new {
        enabled = cloudflareService.IsRunning,
        lastKnownIp = cloudflareService.LastKnownIp ?? "unknown",
        lastUpdate = cloudflareService.LastUpdate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "never",
        status = cloudflareService.IsRunning ? "running" : "stopped"
      };
    }
    catch (Exception ex) {
      return new {
        enabled = false,
        lastKnownIp = "unknown",
        lastUpdate = "never",
        status = "error",
        error = ex.Message
      };
    }
  }

  /// <summary>
  /// Dynamic DNS status endpoint
  /// </summary>
  [HttpGet("dynamic-dns")]
  [Public]
  public async Task<IActionResult> GetDynamicDnsStatusEndpoint() {
    try {
      var status = GetDynamicDnsStatus();
      var currentIp = await cloudflareService.GetExternalIpAsync();

      return Ok(new {
        timestamp = DateTime.UtcNow,
        currentIp = currentIp ?? "unknown",
        service = status
      });
    }
    catch (Exception ex) {
      return StatusCode(
        503,
        new {
          timestamp = DateTime.UtcNow,
          currentIp = "unknown",
          service = new {
            enabled = false,
            status = "error",
            error = ex.Message
          }
        }
      );
    }
  }
}

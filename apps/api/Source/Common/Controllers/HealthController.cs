using GameGuild.Database;
using GameGuild.Modules.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Common;

[ApiController]
[Route("[controller]")]
public class HealthController(ApplicationDbContext context) : ControllerBase {
  /// <summary>
  /// Health check endpoint - publicly accessible for connectivity testing
  /// </summary>
  [HttpGet]
  [Public]
  public async Task<IActionResult> GetHealth() {
    try {
      // Check database connectivity
      await context.Database.CanConnectAsync();

      return Ok(new { status = "healthy", timestamp = DateTime.UtcNow, environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), database = "connected" });
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
}

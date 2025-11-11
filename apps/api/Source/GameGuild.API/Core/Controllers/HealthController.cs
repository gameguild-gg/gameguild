using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameGuild.API.Controllers;

/// <summary>
///     Health check endpoint for monitoring and load balancer health checks
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Health")]
public class HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger) : ControllerBase
{
    private readonly HealthCheckService _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));

    private readonly ILogger<HealthController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Comprehensive health check endpoint for application monitoring
    /// </summary>
    /// <returns>Detailed health status including all registered health checks</returns>
    /// <remarks>
    ///     Performs a comprehensive health check of all registered services and dependencies including:
    ///     - Database connectivity
    ///     - External service availability
    ///     - Cache systems (Redis, etc.)
    ///     - Message queues
    ///     - File systems
    ///     Returns HTTP 200 when all checks are healthy, HTTP 503 when any check fails.
    ///     Used by monitoring systems, load balancers, and orchestration platforms to determine application health.
    /// </remarks>
    [HttpGet]
    [EndpointSummary("Comprehensive application health check")]
    [EndpointDescription("Performs a comprehensive health check of all registered services and dependencies. Returns detailed status information for monitoring systems, load balancers, and orchestration platforms.")]
    [ProducesResponseType<HealthCheckResponse>(200)]
    [ProducesResponseType<HealthCheckResponse>(503)]
    public async Task<ActionResult<HealthCheckResponse>> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new HealthCheckResponse
            {
                Status = healthReport.Status.ToString(),
                Duration = healthReport.TotalDuration,
                Timestamp = DateTime.UtcNow,
                Checks = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HealthCheckItem { Status = kvp.Value.Status.ToString(), Duration = kvp.Value.Duration, Description = kvp.Value.Description, Data = kvp.Value.Data }
                )
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            var errorResponse = new HealthCheckResponse { Status = HealthStatus.Unhealthy.ToString(), Duration = TimeSpan.Zero, Timestamp = DateTime.UtcNow, Error = ex.Message };

            return StatusCode(503, errorResponse);
        }
    }

    /// <summary>
    ///     Kubernetes-style readiness probe for traffic routing decisions
    /// </summary>
    /// <returns>Application readiness status indicating if it's ready to serve traffic</returns>
    /// <remarks>
    ///     Readiness probes determine whether the application is ready to serve traffic.
    ///     Unlike liveness probes, readiness checks verify that all dependencies are available
    ///     and the application can handle requests properly.
    ///     Kubernetes uses this endpoint to:
    ///     - Remove pods from service endpoints when not ready
    ///     - Prevent traffic routing to initializing instances
    ///     - Handle rolling deployments gracefully
    ///     Returns HTTP 200 when ready to serve traffic, HTTP 503 when not ready.
    ///     Checks services tagged with "ready" in health check registration.
    /// </remarks>
    [HttpGet("ready")]
    [EndpointSummary("Readiness probe for traffic routing decisions")]
    [EndpointDescription("Kubernetes-style readiness probe that determines whether the application is ready to serve traffic. Checks all dependencies and services required for proper request handling.")]
    [ProducesResponseType<ReadinessResponse>(200)]
    [ProducesResponseType<ReadinessResponse>(503)]
    public async Task<ActionResult<ReadinessResponse>> GetReadiness()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));

            var response = new ReadinessResponse
            {
                Status = healthReport.Status.ToString(),
                Ready = healthReport.Status == HealthStatus.Healthy,
                Timestamp = DateTime.UtcNow,
                Services = healthReport.Entries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Status == HealthStatus.Healthy)
            };

            var statusCode = response.Ready ? 200 : 503;

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");

            var errorResponse = new ReadinessResponse { Status = HealthStatus.Unhealthy.ToString(), Ready = false, Timestamp = DateTime.UtcNow, Error = ex.Message };

            return StatusCode(503, errorResponse);
        }
    }

    /// <summary>
    ///     Kubernetes-style liveness probe for container restart decisions
    /// </summary>
    /// <returns>Application liveness status indicating if the process is running correctly</returns>
    /// <remarks>
    ///     Liveness probes determine whether the application process is running and functioning.
    ///     This is a lightweight check that verifies the application hasn't deadlocked or crashed.
    ///     Kubernetes uses this endpoint to:
    ///     - Restart containers that are in a broken state
    ///     - Detect application deadlocks or memory leaks
    ///     - Ensure long-running processes remain healthy
    ///     Always returns HTTP 200 if the process is running. Includes:
    ///     - Application uptime since startup
    ///     - Current timestamp
    ///     - Application version information
    ///     - Basic process health indicators
    ///     This check is intentionally simple and should not depend on external services.
    /// </remarks>
    [HttpGet("live")]
    [EndpointSummary("Liveness probe for container restart decisions")]
    [EndpointDescription("Kubernetes-style liveness probe that indicates whether the application process is running correctly. Used by orchestration platforms to determine if containers should be restarted.")]
    [ProducesResponseType<LivenessResponse>(200)]
    public ActionResult<LivenessResponse> GetLiveness()
    {
        var response = new LivenessResponse
        {
            Status = "Healthy",
            Alive = true,
            Timestamp = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown"
        };

        return Ok(response);
    }
}

/// <summary>
///     Health check response model
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public DateTime Timestamp { get; set; }

    public Dictionary<string, HealthCheckItem> Checks { get; set; } = new Dictionary<string, HealthCheckItem>();

    public string? Error { get; set; }
}

/// <summary>
///     Individual health check item
/// </summary>
public class HealthCheckItem
{
    public string Status { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public string? Description { get; set; }

    public IReadOnlyDictionary<string, object>? Data { get; set; }
}

/// <summary>
///     Readiness check response model
/// </summary>
public class ReadinessResponse
{
    public string Status { get; set; } = string.Empty;

    public bool Ready { get; set; }

    public DateTime Timestamp { get; set; }

    public Dictionary<string, bool> Services { get; set; } = new Dictionary<string, bool>();

    public string? Error { get; set; }
}

/// <summary>
///     Liveness check response model
/// </summary>
public class LivenessResponse
{
    public string Status { get; set; } = string.Empty;

    public bool Alive { get; set; }

    public DateTime Timestamp { get; set; }

    public TimeSpan Uptime { get; set; }

    public string Version { get; set; } = string.Empty;
}

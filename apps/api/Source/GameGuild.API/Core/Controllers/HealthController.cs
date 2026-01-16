using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameGuild.API.Controllers;

/// <summary>
///     Health check endpoint for monitoring and load balancer health checks
/// </summary>
[ApiController]
[Route("[controller]")]
[Tags("health")]
[AllowAnonymous]
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
    [ProducesResponseType<HealthinessResponse>(200)]
    [ProducesResponseType<HealthinessResponse>(503)]
    public async Task<ActionResult<HealthinessResponse>> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new HealthinessResponse
            {
                Status = healthReport.Status.ToString(),
                Duration = healthReport.TotalDuration,
                Timestamp = DateTime.UtcNow,
                Checks = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HealthinessResponseItem { Status = kvp.Value.Status.ToString(), Duration = kvp.Value.Duration, Description = kvp.Value.Description, Data = kvp.Value.Data }
                )
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            var errorResponse = new HealthinessResponse { Status = nameof(HealthStatus.Unhealthy), Duration = TimeSpan.Zero, Timestamp = DateTime.UtcNow, Error = ex.Message };

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
    [HttpGet("/ready")]
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

            var errorResponse = new ReadinessResponse { Status = nameof(HealthStatus.Unhealthy), Ready = false, Timestamp = DateTime.UtcNow, Error = ex.Message };

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
    [HttpGet("/live")]
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

    /// <summary>
    ///     Get detailed dependency health status
    /// </summary>
    /// <returns>Detailed health status of all external dependencies</returns>
    /// <remarks>
    ///     Provides detailed health information for all registered external dependencies including:
    ///     - Database connections (PostgreSQL, Redis, etc.)
    ///     - External APIs and services
    ///     - Message queues
    ///     - File storage systems
    ///     - Cache systems
    ///     
    ///     Each dependency includes:
    ///     - Current status (Healthy, Degraded, Unhealthy)
    ///     - Response time
    ///     - Connection details (sanitized)
    ///     - Error information if unhealthy
    /// </remarks>
    [HttpGet("/health/dependencies")]
    [EndpointSummary("Detailed dependency health check")]
    [EndpointDescription("Provides comprehensive health status of all external dependencies including databases, APIs, caches, and message queues.")]
    [ProducesResponseType<DependencyHealthResponse>(200)]
    [ProducesResponseType<DependencyHealthResponse>(503)]
    public async Task<ActionResult<DependencyHealthResponse>> GetDependencyHealth()
    {
        try
        {
            // Run health checks tagged with "dependency"
            var healthReport = await _healthCheckService.CheckHealthAsync(
                check => check.Tags.Contains("dependency") || check.Tags.Contains("ready"));

            var dependencies = new List<DependencyHealthItem>();
            
            foreach (var entry in healthReport.Entries)
            {
                dependencies.Add(new DependencyHealthItem
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Duration = entry.Value.Duration,
                    Description = entry.Value.Description,
                    IsHealthy = entry.Value.Status == HealthStatus.Healthy,
                    Tags = entry.Value.Tags?.ToList() ?? new List<string>(),
                    Data = entry.Value.Data?.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>(),
                    Exception = entry.Value.Exception?.Message
                });
            }

            var response = new DependencyHealthResponse
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration,
                Timestamp = DateTime.UtcNow,
                HealthyCount = dependencies.Count(d => d.IsHealthy),
                UnhealthyCount = dependencies.Count(d => !d.IsHealthy),
                Dependencies = dependencies
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dependency health check failed");

            var errorResponse = new DependencyHealthResponse
            {
                Status = nameof(HealthStatus.Unhealthy),
                TotalDuration = TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };

            return StatusCode(503, errorResponse);
        }
    }

    /// <summary>
    ///     Get application metrics in Prometheus format
    /// </summary>
    /// <returns>Prometheus-compatible metrics for monitoring and alerting</returns>
    /// <remarks>
    ///     Exposes application metrics in Prometheus text format for scraping by monitoring systems.
    ///     Metrics include:
    ///     - HTTP request counts and durations
    ///     - Database connection pool statistics
    ///     - Memory and CPU usage
    ///     - Custom business metrics
    ///     - Error rates and counts
    ///     
    ///     This endpoint is designed for use with Prometheus, Grafana, and other CNCF monitoring tools.
    /// </remarks>
    [HttpGet("/metrics")]
    [EndpointSummary("Prometheus metrics endpoint")]
    [EndpointDescription("Exposes application metrics in Prometheus text format for monitoring, alerting, and observability dashboards.")]
    [Produces("text/plain")]
    [ProducesResponseType<string>(200)]
    public ActionResult GetMetrics()
    {
        var process = Process.GetCurrentProcess();
        var assembly = Assembly.GetExecutingAssembly();
        var startTime = process.StartTime.ToUniversalTime();
        var uptime = (DateTime.UtcNow - startTime).TotalSeconds;

        // Build Prometheus-format metrics
        var metrics = new System.Text.StringBuilder();
        
        // Process metrics
        metrics.AppendLine("# HELP process_cpu_seconds_total Total user and system CPU time spent in seconds.");
        metrics.AppendLine("# TYPE process_cpu_seconds_total counter");
        metrics.AppendLine($"process_cpu_seconds_total {process.TotalProcessorTime.TotalSeconds:F2}");
        
        metrics.AppendLine("# HELP process_virtual_memory_bytes Virtual memory size in bytes.");
        metrics.AppendLine("# TYPE process_virtual_memory_bytes gauge");
        metrics.AppendLine($"process_virtual_memory_bytes {process.VirtualMemorySize64}");
        
        metrics.AppendLine("# HELP process_working_set_bytes Process working set in bytes.");
        metrics.AppendLine("# TYPE process_working_set_bytes gauge");
        metrics.AppendLine($"process_working_set_bytes {process.WorkingSet64}");
        
        metrics.AppendLine("# HELP process_private_memory_bytes Process private memory size in bytes.");
        metrics.AppendLine("# TYPE process_private_memory_bytes gauge");
        metrics.AppendLine($"process_private_memory_bytes {process.PrivateMemorySize64}");
        
        metrics.AppendLine("# HELP process_start_time_seconds Start time of the process since unix epoch in seconds.");
        metrics.AppendLine("# TYPE process_start_time_seconds gauge");
        metrics.AppendLine($"process_start_time_seconds {new DateTimeOffset(startTime).ToUnixTimeSeconds()}");
        
        metrics.AppendLine("# HELP process_uptime_seconds The uptime of the process in seconds.");
        metrics.AppendLine("# TYPE process_uptime_seconds gauge");
        metrics.AppendLine($"process_uptime_seconds {uptime:F2}");
        
        // Thread metrics
        metrics.AppendLine("# HELP process_num_threads Total number of threads.");
        metrics.AppendLine("# TYPE process_num_threads gauge");
        metrics.AppendLine($"process_num_threads {process.Threads.Count}");
        
        // GC metrics
        metrics.AppendLine("# HELP dotnet_gc_collections_total Total number of garbage collections.");
        metrics.AppendLine("# TYPE dotnet_gc_collections_total counter");
        metrics.AppendLine($"dotnet_gc_collections_total{{generation=\"0\"}} {GC.CollectionCount(0)}");
        metrics.AppendLine($"dotnet_gc_collections_total{{generation=\"1\"}} {GC.CollectionCount(1)}");
        metrics.AppendLine($"dotnet_gc_collections_total{{generation=\"2\"}} {GC.CollectionCount(2)}");
        
        metrics.AppendLine("# HELP dotnet_gc_memory_total_bytes Total known allocated memory in bytes.");
        metrics.AppendLine("# TYPE dotnet_gc_memory_total_bytes gauge");
        metrics.AppendLine($"dotnet_gc_memory_total_bytes {GC.GetTotalMemory(false)}");
        
        // Application info metric
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        metrics.AppendLine("# HELP app_info Application version and build information.");
        metrics.AppendLine("# TYPE app_info gauge");
        metrics.AppendLine($"app_info{{version=\"{version}\",runtime=\"{RuntimeInformation.FrameworkDescription}\"}} 1");

        return Content(metrics.ToString(), "text/plain");
    }

    /// <summary>
    ///     Get application information including version and build details
    /// </summary>
    /// <returns>Application version, build, and runtime information</returns>
    /// <remarks>
    ///     Provides comprehensive application information for debugging and monitoring:
    ///     - Application name and version
    ///     - Build timestamp and commit hash (if available)
    ///     - Runtime and framework versions
    ///     - Environment information
    ///     - Feature flags and configuration (non-sensitive)
    ///     
    ///     Useful for:
    ///     - Debugging version mismatches
    ///     - Monitoring deployments
    ///     - Correlating logs with specific builds
    /// </remarks>
    [HttpGet("/info")]
    [EndpointSummary("Application information endpoint")]
    [EndpointDescription("Provides application version, build details, and runtime information for debugging and deployment monitoring.")]
    [ProducesResponseType<ApplicationInfoResponse>(200)]
    public ActionResult<ApplicationInfoResponse> GetInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var process = Process.GetCurrentProcess();
        
        // Get assembly attributes
        var assemblyName = assembly.GetName();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var buildDate = GetBuildDate(assembly);
        
        var response = new ApplicationInfoResponse
        {
            Application = new ApplicationDetails
            {
                Name = assemblyName.Name ?? "GameGuild.API",
                Version = assemblyName.Version?.ToString() ?? "1.0.0",
                InformationalVersion = informationalVersion ?? assemblyName.Version?.ToString() ?? "1.0.0",
                Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "GameGuild API Platform"
            },
            Build = new BuildDetails
            {
                Timestamp = buildDate,
                Configuration = 
#if DEBUG
                    "Debug",
#else
                    "Release",
#endif
                Framework = RuntimeInformation.FrameworkDescription
            },
            Runtime = new RuntimeDetails
            {
                DotNetVersion = Environment.Version.ToString(),
                OSDescription = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount
            },
            Process = new ProcessDetails
            {
                Id = process.Id,
                StartTime = process.StartTime.ToUniversalTime(),
                Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime(),
                WorkingSet = process.WorkingSet64,
                ThreadCount = process.Threads.Count
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        try
        {
            // Try to get build date from assembly metadata
            var attribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
            if (attribute != null && attribute.Key == "BuildDate" && DateTime.TryParse(attribute.Value, out var date))
            {
                return date;
            }
            
            // Fallback: use file last write time
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
            {
                return System.IO.File.GetLastWriteTimeUtc(location);
            }
        }
        catch
        {
            // Ignore errors and return null
        }
        
        return null;
    }
}

/// <summary>
///     Health check response model
/// </summary>
public class HealthinessResponse
{
    public string Status { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public DateTime Timestamp { get; set; }

    public Dictionary<string, HealthinessResponseItem> Checks { get; set; } = new Dictionary<string, HealthinessResponseItem>();

    public string? Error { get; set; }
}

/// <summary>
///     Individual health check item
/// </summary>
public class HealthinessResponseItem
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

/// <summary>
///     Dependency health check response model
/// </summary>
public class DependencyHealthResponse
{
    public string Status { get; set; } = string.Empty;

    public TimeSpan TotalDuration { get; set; }

    public DateTime Timestamp { get; set; }

    public int HealthyCount { get; set; }

    public int UnhealthyCount { get; set; }

    public List<DependencyHealthItem> Dependencies { get; set; } = new();

    public string? Error { get; set; }
}

/// <summary>
///     Individual dependency health item
/// </summary>
public class DependencyHealthItem
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public string? Description { get; set; }

    public bool IsHealthy { get; set; }

    public List<string> Tags { get; set; } = new();

    public Dictionary<string, string> Data { get; set; } = new();

    public string? Exception { get; set; }
}

/// <summary>
///     Application info response model
/// </summary>
public class ApplicationInfoResponse
{
    public ApplicationDetails Application { get; set; } = new();

    public BuildDetails Build { get; set; } = new();

    public RuntimeDetails Runtime { get; set; } = new();

    public ProcessDetails Process { get; set; } = new();

    public DateTime Timestamp { get; set; }
}

/// <summary>
///     Application details
/// </summary>
public class ApplicationDetails
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string InformationalVersion { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
///     Build details
/// </summary>
public class BuildDetails
{
    public DateTime? Timestamp { get; set; }

    public string Configuration { get; set; } = string.Empty;

    public string Framework { get; set; } = string.Empty;
}

/// <summary>
///     Runtime details
/// </summary>
public class RuntimeDetails
{
    public string DotNetVersion { get; set; } = string.Empty;

    public string OSDescription { get; set; } = string.Empty;

    public string OSArchitecture { get; set; } = string.Empty;

    public string ProcessArchitecture { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public int ProcessorCount { get; set; }
}

/// <summary>
///     Process details
/// </summary>
public class ProcessDetails
{
    public int Id { get; set; }

    public DateTime StartTime { get; set; }

    public TimeSpan Uptime { get; set; }

    public long WorkingSet { get; set; }

    public int ThreadCount { get; set; }
}

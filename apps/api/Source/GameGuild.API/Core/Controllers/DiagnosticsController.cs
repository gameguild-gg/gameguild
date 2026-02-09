using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Controllers;

/// <summary>
///     Application diagnostics endpoint providing version, build, and runtime information.
/// </summary>
[ApiController]
[Tags("health")]
[Authorize]
public class DiagnosticsController(ILogger<DiagnosticsController> logger) : ControllerBase
{
    private readonly ILogger<DiagnosticsController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

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
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var buildDate = GetBuildDate(assembly);

        var response = new ApplicationInfoResponse
        {
            Application = new ApplicationDetails
            {
                Name = assemblyName.Name ?? "GameGuild.API",
                Version = assemblyName.Version?.ToString() ?? "1.0.0",
                InformationalVersion = informationalVersion ?? assemblyName.Version?.ToString() ?? "1.0.0",
                Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
                              ?? "GameGuild API Platform"
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
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
            },
            Process = new ProcessDetails
            {
                StartTime = process.StartTime.ToUniversalTime(),
                Uptime = SystemClock.UtcNow - process.StartTime.ToUniversalTime()
            },
            Timestamp = SystemClock.UtcNow
        };

        return Ok(response);
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        try
        {
            // Try to get build date from assembly metadata
            var attribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
            if (attribute != null
                && attribute.Key == "BuildDate"
                && DateTime.TryParse(attribute.Value, out var date))
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

#region Response Models

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
}

/// <summary>
///     Process details
/// </summary>
public class ProcessDetails
{
    public DateTime StartTime { get; set; }

    public TimeSpan Uptime { get; set; }
}

#endregion

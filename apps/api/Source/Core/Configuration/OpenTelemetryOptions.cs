namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration options for OpenTelemetry
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Application environment name
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Enable console exporter for development
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = true;

    /// <summary>
    /// Enable OTLP exporter for production
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = false;

    /// <summary>
    /// OTLP endpoint URL
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// OTLP headers for authentication
    /// </summary>
    public string? OtlpHeaders { get; set; }

    /// <summary>
    /// Validates the options
    /// </summary>
    public void Validate()
    {
        if (EnableOtlpExporter && string.IsNullOrEmpty(OtlpEndpoint)) { throw new InvalidOperationException("OtlpEndpoint must be specified when EnableOtlpExporter is true"); }
    }
}
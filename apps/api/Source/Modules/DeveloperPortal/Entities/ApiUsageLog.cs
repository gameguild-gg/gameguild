using GameGuild.Core.Domain;

namespace GameGuild.Modules.DeveloperPortal.Entities;

/// <summary>
/// Represents a log entry for API usage tracking.
/// </summary>
public class ApiUsageLog : EntityBase {
    /// <summary>
    /// Gets or sets the API key used for this request.
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the API key.
    /// </summary>
    public ApiKey? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint path.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the request size in bytes.
    /// </summary>
    public long? RequestSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the response size in bytes.
    /// </summary>
    public long? ResponseSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the caller.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the referrer URL.
    /// </summary>
    public string? Referrer { get; set; }

    /// <summary>
    /// Gets or sets whether the request was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    // Backward compatibility alias
    public string Method { get => HttpMethod; set => HttpMethod = value; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the request was made.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the SDK version used.
    /// </summary>
    public string? SdkVersion { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}

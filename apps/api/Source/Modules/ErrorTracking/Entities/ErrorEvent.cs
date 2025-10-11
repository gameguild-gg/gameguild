using GameGuild.Core.Domain;

namespace GameGuild.Modules.ErrorTracking.Entities;

/// <summary>
/// Represents an error event captured in the system (Sentry-style).
/// </summary>
public class ErrorEvent : EntityBase
{
    /// <summary>
    /// Gets or sets the tenant ID this error belongs to.
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the unique fingerprint for grouping similar errors.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error issue this event belongs to.
    /// </summary>
    public Guid ErrorIssueId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the error issue.
    /// </summary>
    public ErrorIssue? ErrorIssue { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception type (e.g., NullReferenceException).
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

    /// <summary>
    /// Gets or sets the environment where the error occurred.
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    /// Gets or sets the release/version where the error occurred.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// Gets or sets the user ID who encountered the error.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the request URL where the error occurred.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the user.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets custom tags as JSON.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets additional context data as JSON.
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Gets or sets the breadcrumbs (user actions before error) as JSON.
    /// </summary>
    public string? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets when the error occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this error has been resolved.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Gets or sets when the error was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Represents the severity level of an error.
/// </summary>
public enum ErrorSeverity
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4
}

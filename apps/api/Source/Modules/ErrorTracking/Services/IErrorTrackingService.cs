namespace GameGuild.Modules.ErrorTracking.Services;

/// <summary>
/// Service for tracking and aggregating errors (Sentry-style).
/// </summary>
public interface IErrorTrackingService
{
    /// <summary>
    /// Captures an error event and groups it into an issue.
    /// </summary>
    Task<Guid> CaptureErrorAsync(CaptureErrorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an error issue by ID.
    /// </summary>
    Task<ErrorIssueDto?> GetIssueAsync(Guid issueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all error issues with optional filtering.
    /// </summary>
    Task<IEnumerable<ErrorIssueDto>> GetIssuesAsync(GetIssuesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets error events for a specific issue.
    /// </summary>
    Task<IEnumerable<ErrorEventDto>> GetIssueEventsAsync(Guid issueId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an error issue.
    /// </summary>
    Task ResolveIssueAsync(Guid issueId, Guid userId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens a resolved issue.
    /// </summary>
    Task ReopenIssueAsync(Guid issueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ignores an error issue (won't trigger alerts).
    /// </summary>
    Task IgnoreIssueAsync(Guid issueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mutes alerts for an issue until a specific time.
    /// </summary>
    Task MuteIssueAsync(Guid issueId, DateTime until, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns an issue to a user.
    /// </summary>
    Task AssignIssueAsync(Guid issueId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an error issue and all its events.
    /// </summary>
    Task DeleteIssueAsync(Guid issueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets error statistics for a time period.
    /// </summary>
    Task<ErrorStatisticsDto> GetStatisticsAsync(Guid? tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique fingerprint for an error event.
    /// </summary>
    string GenerateFingerprint(string exceptionType, string message, string? stackTrace);
}

/// <summary>
/// Request for capturing an error.
/// </summary>
public record CaptureErrorRequest(
    Guid? TenantId,
    string Message,
    string ExceptionType,
    string? StackTrace,
    string Severity,
    string Environment,
    string? Release,
    Guid? UserId,
    string? Url,
    string? HttpMethod,
    string? UserAgent,
    string? IpAddress,
    Dictionary<string, string>? Tags,
    Dictionary<string, object>? ContextData,
    List<Dictionary<string, object>>? Breadcrumbs
);

/// <summary>
/// Request for querying error issues.
/// </summary>
public record GetIssuesRequest(
    Guid? TenantId = null,
    string? Status = null,
    string? Severity = null,
    string? Environment = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Skip = 0,
    int Take = 50
);

/// <summary>
/// DTO for error issue information.
/// </summary>
public record ErrorIssueDto(
    Guid Id,
    Guid? TenantId,
    string Fingerprint,
    string Title,
    string ExceptionType,
    string Message,
    string Status,
    int EventCount,
    int UserCount,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    string Severity,
    string? Environments,
    string? Releases,
    Guid? AssignedToUserId,
    DateTime? ResolvedAt,
    Guid? ResolvedByUserId,
    string? ResolutionNotes,
    bool IsMuted,
    DateTime? MutedUntil
);

/// <summary>
/// DTO for error event information.
/// </summary>
public record ErrorEventDto(
    Guid Id,
    Guid ErrorIssueId,
    string Message,
    string ExceptionType,
    string? StackTrace,
    string Severity,
    string Environment,
    string? Release,
    Guid? UserId,
    string? Url,
    string? HttpMethod,
    string? UserAgent,
    string? IpAddress,
    DateTime OccurredAt
);

/// <summary>
/// DTO for error statistics.
/// </summary>
public record ErrorStatisticsDto(
    int TotalIssues,
    int UnresolvedIssues,
    int ResolvedIssues,
    int TotalEvents,
    int UniqueUsers,
    Dictionary<string, int> EventsBySeverity,
    Dictionary<string, int> EventsByEnvironment,
    List<ErrorTrendDataPoint> Trend
);

/// <summary>
/// Represents a data point in an error trend.
/// </summary>
public record ErrorTrendDataPoint(
    DateTime Timestamp,
    int EventCount,
    int IssueCount
);

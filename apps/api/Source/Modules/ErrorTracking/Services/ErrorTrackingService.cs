using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild;
using GameGuild.Modules.ErrorTracking.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.ErrorTracking.Services;

/// <summary>
/// Service implementation for tracking and aggregating errors (Sentry-style).
/// </summary>
public class ErrorTrackingService : IErrorTrackingService
{
    private readonly IErrorEventRepository _eventRepository;
    private readonly IErrorIssueRepository _issueRepository;
    private readonly IErrorAggregationService _aggregationService;
    private readonly ILogger<ErrorTrackingService> _logger;

    public ErrorTrackingService(
        IErrorEventRepository eventRepository,
        IErrorIssueRepository issueRepository,
        IErrorAggregationService aggregationService,
        ILogger<ErrorTrackingService> logger)
    {
        _eventRepository = eventRepository;
        _issueRepository = issueRepository;
        _aggregationService = aggregationService;
        _logger = logger;
    }

    public async Task<Guid> CaptureErrorAsync(CaptureErrorRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate fingerprint for grouping
            var fingerprint = GenerateFingerprint(request.ExceptionType, request.Message, request.StackTrace);

            // Find or create issue
            var issue = await _aggregationService.FindOrCreateIssueAsync(
                fingerprint,
                request.TenantId,
                request.ExceptionType,
                request.Message,
                request.Severity,
                request.Environment,
                request.Release,
                cancellationToken);

            // Create error event
            var errorEvent = new ErrorEvent
            {
                TenantId = request.TenantId,
                Fingerprint = fingerprint,
                ErrorIssueId = issue.Id,
                Message = request.Message,
                ExceptionType = request.ExceptionType,
                StackTrace = request.StackTrace,
                Severity = Enum.Parse<ErrorSeverity>(request.Severity, true),
                Environment = request.Environment,
                Release = request.Release,
                UserId = request.UserId,
                Url = request.Url,
                HttpMethod = request.HttpMethod,
                UserAgent = request.UserAgent,
                IpAddress = request.IpAddress,
                Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
                ContextData = request.ContextData != null ? JsonSerializer.Serialize(request.ContextData) : null,
                Breadcrumbs = request.Breadcrumbs != null ? JsonSerializer.Serialize(request.Breadcrumbs) : null,
                OccurredAt = DateTime.UtcNow
            };

            await _eventRepository.AddAsync(errorEvent, cancellationToken);

            // Update issue statistics
            await _aggregationService.UpdateIssueStatisticsAsync(issue.Id, request.UserId, cancellationToken);

            _logger.LogInformation("Captured error event {EventId} for issue {IssueId}", errorEvent.Id, issue.Id);

            return errorEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture error for {ExceptionType}: {Message}", request.ExceptionType, request.Message);
            throw;
        }
    }

    public async Task<ErrorIssueDto?> GetIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        return issue != null ? MapToDto(issue) : null;
    }

    public async Task<IEnumerable<ErrorIssueDto>> GetIssuesAsync(GetIssuesRequest request, CancellationToken cancellationToken = default)
    {
        var issues = await _issueRepository.GetAllAsync(
            request.TenantId,
            request.Status,
            request.Severity,
            request.Environment,
            request.StartDate,
            request.EndDate,
            request.Skip,
            request.Take,
            cancellationToken);

        return issues.Select(MapToDto);
    }

    public async Task<IEnumerable<ErrorEventDto>> GetIssueEventsAsync(Guid issueId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var events = await _eventRepository.GetByIssueIdAsync(issueId, skip, take, cancellationToken);
        return events.Select(MapEventToDto);
    }

    public async Task ResolveIssueAsync(Guid issueId, Guid userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue == null)
        {
            throw new InvalidOperationException($"Error issue {issueId} not found");
        }

        issue.Resolve(userId, notes);
        await _issueRepository.UpdateAsync(issue, cancellationToken);

        _logger.LogInformation("Resolved error issue {IssueId} by user {UserId}", issueId, userId);
    }

    public async Task ReopenIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue == null)
        {
            throw new InvalidOperationException($"Error issue {issueId} not found");
        }

        issue.Reopen();
        await _issueRepository.UpdateAsync(issue, cancellationToken);

        _logger.LogInformation("Reopened error issue {IssueId}", issueId);
    }

    public async Task IgnoreIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue == null)
        {
            throw new InvalidOperationException($"Error issue {issueId} not found");
        }

        issue.Ignore();
        await _issueRepository.UpdateAsync(issue, cancellationToken);

        _logger.LogInformation("Ignored error issue {IssueId}", issueId);
    }

    public async Task MuteIssueAsync(Guid issueId, DateTime until, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue == null)
        {
            throw new InvalidOperationException($"Error issue {issueId} not found");
        }

        issue.Mute(until);
        await _issueRepository.UpdateAsync(issue, cancellationToken);

        _logger.LogInformation("Muted error issue {IssueId} until {Until}", issueId, until);
    }

    public async Task AssignIssueAsync(Guid issueId, Guid userId, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue == null)
        {
            throw new InvalidOperationException($"Error issue {issueId} not found");
        }

        issue.Assign(userId);
        await _issueRepository.UpdateAsync(issue, cancellationToken);

        _logger.LogInformation("Assigned error issue {IssueId} to user {UserId}", issueId, userId);
    }

    public async Task DeleteIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        await _eventRepository.DeleteByIssueIdAsync(issueId, cancellationToken);
        await _issueRepository.DeleteAsync(issueId, cancellationToken);

        _logger.LogInformation("Deleted error issue {IssueId} and all its events", issueId);
    }

    public async Task<ErrorStatisticsDto> GetStatisticsAsync(Guid? tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var issues = await _issueRepository.GetAllAsync(tenantId, null, null, null, startDate, endDate, 0, int.MaxValue, cancellationToken);
        var events = await _eventRepository.GetByDateRangeAsync(tenantId, startDate, endDate, cancellationToken);

        var totalIssues = issues.Count();
        var unresolvedIssues = issues.Count(i => i.Status == IssueStatus.Unresolved);
        var resolvedIssues = issues.Count(i => i.Status == IssueStatus.Resolved);
        var totalEvents = events.Count();
        var uniqueUsers = events.Where(e => e.UserId.HasValue).Select(e => e.UserId).Distinct().Count();

        var eventsBySeverity = events
            .GroupBy(e => e.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var eventsByEnvironment = events
            .GroupBy(e => e.Environment)
            .ToDictionary(g => g.Key, g => g.Count());

        var trend = GenerateTrend(events, startDate, endDate);

        return new ErrorStatisticsDto(
            totalIssues,
            unresolvedIssues,
            resolvedIssues,
            totalEvents,
            uniqueUsers,
            eventsBySeverity,
            eventsByEnvironment,
            trend
        );
    }

    public string GenerateFingerprint(string exceptionType, string message, string? stackTrace)
    {
        // Create a unique fingerprint based on exception type, message pattern, and stack trace top frames
        var fingerprintData = new StringBuilder();
        fingerprintData.Append(exceptionType);

        // Normalize message (remove dynamic data like IDs, timestamps)
        var normalizedMessage = NormalizeMessage(message);
        fingerprintData.Append("|");
        fingerprintData.Append(normalizedMessage);

        // Use top 3 stack frames for fingerprinting
        if (!string.IsNullOrEmpty(stackTrace))
        {
            var frames = stackTrace.Split('\n').Take(3);
            fingerprintData.Append("|");
            fingerprintData.Append(string.Join("|", frames.Select(f => f.Trim())));
        }

        // Generate SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintData.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private string NormalizeMessage(string message)
    {
        // Remove GUIDs
        message = System.Text.RegularExpressions.Regex.Replace(message, @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}", "[ID]");

        // Remove numbers that might be IDs or counts
        message = System.Text.RegularExpressions.Regex.Replace(message, @"\b\d+\b", "[NUM]");

        // Remove timestamps
        message = System.Text.RegularExpressions.Regex.Replace(message, @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", "[TIMESTAMP]");

        return message;
    }

    private List<ErrorTrendDataPoint> GenerateTrend(IEnumerable<ErrorEvent> events, DateTime startDate, DateTime endDate)
    {
        var trend = new List<ErrorTrendDataPoint>();
        var duration = endDate - startDate;
        var intervalHours = duration.TotalHours > 168 ? 24 : duration.TotalHours > 24 ? 6 : 1; // Daily for >week, 6h for >day, hourly otherwise

        for (var date = startDate; date < endDate; date = date.AddHours(intervalHours))
        {
            var intervalEnd = date.AddHours(intervalHours);
            var intervalEvents = events.Where(e => e.OccurredAt >= date && e.OccurredAt < intervalEnd).ToList();

            trend.Add(new ErrorTrendDataPoint(
                date,
                intervalEvents.Count,
                intervalEvents.Select(e => e.ErrorIssueId).Distinct().Count()
            ));
        }

        return trend;
    }

    private ErrorIssueDto MapToDto(ErrorIssue issue)
    {
        return new ErrorIssueDto(
            issue.Id,
            issue.TenantId,
            issue.Fingerprint,
            issue.Title,
            issue.ExceptionType,
            issue.Message,
            issue.Status.ToString(),
            issue.EventCount,
            issue.UserCount,
            issue.FirstSeenAt,
            issue.LastSeenAt,
            issue.Severity.ToString(),
            issue.Environments,
            issue.Releases,
            issue.AssignedToUserId,
            issue.ResolvedAt,
            issue.ResolvedByUserId,
            issue.ResolutionNotes,
            issue.IsMuted,
            issue.MutedUntil
        );
    }

    private ErrorEventDto MapEventToDto(ErrorEvent errorEvent)
    {
        return new ErrorEventDto(
            errorEvent.Id,
            errorEvent.ErrorIssueId,
            errorEvent.Message,
            errorEvent.ExceptionType,
            errorEvent.StackTrace,
            errorEvent.Severity.ToString(),
            errorEvent.Environment,
            errorEvent.Release,
            errorEvent.UserId,
            errorEvent.Url,
            errorEvent.HttpMethod,
            errorEvent.UserAgent,
            errorEvent.IpAddress,
            errorEvent.OccurredAt
        );
    }
}

/// <summary>
/// Repository interface for error events.
/// </summary>
public interface IErrorEventRepository
{
    Task<ErrorEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEvent>> GetByIssueIdAsync(Guid issueId, int skip, int take, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEvent>> GetByDateRangeAsync(Guid? tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task AddAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default);
    Task DeleteByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for error issues.
/// </summary>
public interface IErrorIssueRepository
{
    Task<ErrorIssue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorIssue?> GetByFingerprintAsync(string fingerprint, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorIssue>> GetAllAsync(Guid? tenantId, string? status, string? severity, string? environment, DateTime? startDate, DateTime? endDate, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(ErrorIssue issue, CancellationToken cancellationToken = default);
    Task UpdateAsync(ErrorIssue issue, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

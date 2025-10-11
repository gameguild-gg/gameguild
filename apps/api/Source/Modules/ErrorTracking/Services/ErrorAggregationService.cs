using GameGuild.Modules.ErrorTracking.Entities;


namespace GameGuild.Modules.ErrorTracking.Services;

/// <summary>
/// Service for aggregating errors into issues.
/// </summary>
public interface IErrorAggregationService
{
    Task<ErrorIssue> FindOrCreateIssueAsync(string fingerprint, Guid? tenantId, string exceptionType, string message, string severity, string environment, string? release, CancellationToken cancellationToken = default);
    Task UpdateIssueStatisticsAsync(Guid issueId, Guid? userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service implementation for aggregating errors into issues.
/// </summary>
public class ErrorAggregationService : IErrorAggregationService
{
    private readonly IErrorIssueRepository _issueRepository;
    private readonly ILogger<ErrorAggregationService> _logger;

    public ErrorAggregationService(
        IErrorIssueRepository issueRepository,
        ILogger<ErrorAggregationService> logger)
    {
        _issueRepository = issueRepository;
        _logger = logger;
    }

    public async Task<ErrorIssue> FindOrCreateIssueAsync(
        string fingerprint,
        Guid? tenantId,
        string exceptionType,
        string message,
        string severity,
        string environment,
        string? release,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing issue
        var existingIssue = await _issueRepository.GetByFingerprintAsync(fingerprint, tenantId, cancellationToken);
        
        if (existingIssue != null)
        {
            // Update environment and release tracking
            UpdateEnvironments(existingIssue, environment);
            UpdateReleases(existingIssue, release);
            
            // If issue was resolved and error happens again, reopen it (regression)
            if (existingIssue.Status == IssueStatus.Resolved)
            {
                _logger.LogWarning("Error issue {IssueId} regressed - reopening", existingIssue.Id);
                existingIssue.Reopen();
                existingIssue.Status = IssueStatus.Regressed;
            }

            await _issueRepository.UpdateAsync(existingIssue, cancellationToken);
            return existingIssue;
        }

        // Create new issue
        var title = GenerateTitle(exceptionType, message);
        var parsedSeverity = Enum.Parse<ErrorSeverity>(severity, true);

        var newIssue = new ErrorIssue
        {
            TenantId = tenantId,
            Fingerprint = fingerprint,
            Title = title,
            ExceptionType = exceptionType,
            Message = message,
            Status = IssueStatus.Unresolved,
            Severity = parsedSeverity,
            Environments = environment,
            Releases = release,
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            EventCount = 0,
            UserCount = 0
        };

        await _issueRepository.AddAsync(newIssue, cancellationToken);
        
        _logger.LogInformation("Created new error issue {IssueId} for {ExceptionType}", newIssue.Id, exceptionType);

        return newIssue;
    }

    public async Task UpdateIssueStatisticsAsync(Guid issueId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue == null)
        {
            _logger.LogWarning("Cannot update statistics - issue {IssueId} not found", issueId);
            return;
        }

        issue.RecordEvent(userId);
        await _issueRepository.UpdateAsync(issue, cancellationToken);
    }

    private string GenerateTitle(string exceptionType, string message)
    {
        // Create a concise title (max 200 chars)
        var title = $"{exceptionType}: {message}";
        if (title.Length > 200)
        {
            title = title.Substring(0, 197) + "...";
        }
        return title;
    }

    private void UpdateEnvironments(ErrorIssue issue, string environment)
    {
        if (string.IsNullOrEmpty(issue.Environments))
        {
            issue.Environments = environment;
            return;
        }

        var environments = issue.Environments.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (!environments.Contains(environment))
        {
            environments.Add(environment);
            issue.Environments = string.Join(",", environments.OrderBy(e => e));
        }
    }

    private void UpdateReleases(ErrorIssue issue, string? release)
    {
        if (string.IsNullOrEmpty(release))
        {
            return;
        }

        if (string.IsNullOrEmpty(issue.Releases))
        {
            issue.Releases = release;
            return;
        }

        var releases = issue.Releases.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (!releases.Contains(release))
        {
            releases.Add(release);
            issue.Releases = string.Join(",", releases.OrderBy(r => r));
        }
    }
}

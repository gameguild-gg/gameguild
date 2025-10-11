using GameGuild.Modules.ErrorTracking.Entities;
using GameGuild.Modules.ErrorTracking.Services;


namespace GameGuild.Modules.ErrorTracking.Repositories;

/// <summary>
/// Repository for ErrorIssue entities.
/// </summary>
public class ErrorIssueRepository : IErrorIssueRepository
{
    private readonly DbContext _context;

    public ErrorIssueRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<ErrorIssue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorIssue>()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<ErrorIssue?> GetByFingerprintAsync(string fingerprint, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorIssue>()
            .FirstOrDefaultAsync(i => i.Fingerprint == fingerprint && i.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<ErrorIssue>> GetAllAsync(
        Guid? tenantId,
        string? status,
        string? severity,
        string? environment,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ErrorIssue>().AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(i => i.TenantId == tenantId);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<IssueStatus>(status, true, out var issueStatus))
        {
            query = query.Where(i => i.Status == issueStatus);
        }

        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<ErrorSeverity>(severity, true, out var errorSeverity))
        {
            query = query.Where(i => i.Severity == errorSeverity);
        }

        if (!string.IsNullOrEmpty(environment))
        {
            query = query.Where(i => i.Environments != null && i.Environments.Contains(environment));
        }

        if (startDate.HasValue)
        {
            query = query.Where(i => i.FirstSeenAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.LastSeenAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(i => i.LastSeenAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ErrorIssue issue, CancellationToken cancellationToken = default)
    {
        _context.Set<ErrorIssue>().Add(issue);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ErrorIssue issue, CancellationToken cancellationToken = default)
    {
        _context.Set<ErrorIssue>().Update(issue);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var issue = await GetByIdAsync(id, cancellationToken);
        if (issue != null)
        {
            _context.Set<ErrorIssue>().Remove(issue);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

using GameGuild.Modules.ErrorTracking.Entities;
using GameGuild.Modules.ErrorTracking.Services;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.ErrorTracking.Repositories;

/// <summary>
/// Repository for ErrorEvent entities.
/// </summary>
public class ErrorEventRepository : IErrorEventRepository
{
    private readonly DbContext _context;

    public ErrorEventRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<ErrorEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorEvent>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ErrorEvent>> GetByIssueIdAsync(Guid issueId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorEvent>()
            .Where(e => e.ErrorIssueId == issueId)
            .OrderByDescending(e => e.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ErrorEvent>> GetByDateRangeAsync(Guid? tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ErrorEvent>().AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId);
        }

        return await query
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default)
    {
        _context.Set<ErrorEvent>().Add(errorEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var events = await _context.Set<ErrorEvent>()
            .Where(e => e.ErrorIssueId == issueId)
            .ToListAsync(cancellationToken);

        _context.Set<ErrorEvent>().RemoveRange(events);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

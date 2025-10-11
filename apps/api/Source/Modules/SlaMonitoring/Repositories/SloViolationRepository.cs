using GameGuild.Database;
using Microsoft.EntityFrameworkCore;
using GameGuild.Modules.SlaMonitoring.Entities;
using GameGuild.Database;

namespace GameGuild.Modules.SlaMonitoring.Repositories;

/// <summary>
/// Repository implementation for SLO Violations using EF Core.
/// </summary>
public class SloViolationRepository : ISloViolationRepository
{
    private readonly ApplicationDbContext _context;

    public SloViolationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SloViolation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SloViolations
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(SloViolation violation, CancellationToken cancellationToken = default)
    {
        await _context.SloViolations.AddAsync(violation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SloViolation violation, CancellationToken cancellationToken = default)
    {
        _context.SloViolations.Update(violation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<SloViolation>> GetBySloIdAsync(Guid sloId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.SloViolations
            .Where(v => v.SloId == sloId && v.StartedAt >= startDate && v.StartedAt <= endDate)
            .OrderByDescending(v => v.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SloViolation>> GetActiveBySloIdAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _context.SloViolations
            .Where(v => v.SloId == sloId && !v.IsResolved)
            .OrderByDescending(v => v.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SloViolation>> GetActiveViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SloViolations.Where(v => !v.IsResolved);

        if (tenantId.HasValue)
            query = query.Where(v => v.TenantId == tenantId.Value);

        return await query
            .OrderByDescending(v => v.StartedAt)
            .ToListAsync(cancellationToken);
    }
}




using Microsoft.EntityFrameworkCore;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Repository implementation for SLO Violation entities.
/// </summary>
public class SloViolationRepository(DbContext context) : ISloViolationRepository
{
    private readonly DbSet<SloViolation> _violations = context.Set<SloViolation>();

    public async Task<SloViolation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _violations.FirstOrDefaultAsync(v => v.Id == id, cancellationToken); }

    public async Task<List<SloViolation>> GetBySloIdAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.ServiceLevelObjectiveId == sloId).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetBySloIdAndTimeRangeAsync(Guid sloId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.ServiceLevelObjectiveId == sloId && v.StartedAt >= startDate && v.StartedAt <= endDate).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.TenantId != null && v.TenantId.Value == tenantId).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetOngoingViolationsAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.ServiceLevelObjectiveId == sloId && v.EndedAt == null).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetAllOngoingViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _violations.Where(v => v.EndedAt == null);

        if (tenantId.HasValue) { query = query.Where(v => v.TenantId != null && v.TenantId.Value == tenantId.Value); }

        return await query.OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetBySeverityAsync(ViolationSeverity severity, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.Severity == severity && v.TenantId != null && v.TenantId.Value == tenantId).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetUnacknowledgedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.IsAcknowledged == false && v.TenantId != null && v.TenantId.Value == tenantId).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<SloViolation>> GetWithAlertsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _violations.Where(v => v.AlertTriggered == true && v.TenantId != null && v.TenantId.Value == tenantId).OrderByDescending(v => v.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<SloViolation> AddAsync(SloViolation violation, CancellationToken cancellationToken = default)
    {
        await _violations.AddAsync(violation, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return violation;
    }

    public async Task UpdateAsync(SloViolation violation, CancellationToken cancellationToken = default)
    {
        _violations.Update(violation);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountViolationsAsync(Guid sloId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        return await _violations.CountAsync(v => v.ServiceLevelObjectiveId == sloId && v.StartedAt >= startDate && v.StartedAt <= endDate, cancellationToken);
    }
}

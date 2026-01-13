using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository for audit trail entries
/// </summary>
public class AuditTrailRepository(IApplicationDbContext context) : IAuditTrailRepository
{
    private DbSet<AuditTrail> AuditTrails { get => context.Set<AuditTrail>(); }

    public async Task<List<AuditTrail>> GetByEntityAsync(string entityType, Guid entityId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await AuditTrails.Where(a => a.EntityType == entityType && a.EntityId == entityId).OrderByDescending(a => a.ChangedAt).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(AuditTrail auditTrail, CancellationToken cancellationToken = default) { await AuditTrails.AddAsync(auditTrail, cancellationToken).ConfigureAwait(false); }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}

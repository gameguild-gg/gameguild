using GameGuild.Compliance.Audit;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Database;

/// <summary>
///     Host-backed reader over the universal entity-change audit records (issue #166). The record
///     entity lives in the host's database project, so the compliance audit module queries it through
///     <see cref="IEntityChangeAuditStore"/>: captured changes are served through the centralized
///     audit surface without the module referencing the host type.
/// </summary>
public sealed class EntityChangeAuditStore(ApplicationDbContext context) : IEntityChangeAuditStore
{
    private const int MaxTake = 200;

    public async Task<IReadOnlyList<EntityChangeAuditEntry>> QueryAsync(EntityChangeAuditQuery query, CancellationToken cancellationToken)
    {
        return await ApplyFilters(context.Set<EntityChangeAuditRecord>().AsNoTracking(), query)
            .OrderByDescending(record => record.OccurredAtUtc)
            .ThenByDescending(record => record.Id)
            .Skip(Math.Max(0, query.Skip))
            .Take(NormalizeTake(query.Take))
            .Select(record => new EntityChangeAuditEntry(
                record.Id,
                record.EntityName,
                record.KeyId,
                record.ChangeKind,
                record.BeforeValues,
                record.AfterValues,
                record.TenantId,
                record.ActorId,
                record.CorrelationId,
                record.OccurredAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountAsync(EntityChangeAuditQuery query, CancellationToken cancellationToken) =>
        ApplyFilters(context.Set<EntityChangeAuditRecord>().AsNoTracking(), query)
            .CountAsync(cancellationToken);

    private static IQueryable<EntityChangeAuditRecord> ApplyFilters(IQueryable<EntityChangeAuditRecord> records, EntityChangeAuditQuery query)
    {
        // Tenant scoping is mandatory: the query surface never crosses tenant boundaries.
        if (query.TenantId is { } tenant)
        {
            records = records.Where(record => record.TenantId == tenant);
        }
        else
        {
            records = records.Where(record => record.TenantId == null);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            records = records.Where(record => record.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.ChangeKind))
        {
            records = records.Where(record => record.ChangeKind == query.ChangeKind);
        }

        if (query.ActorId is { } actor)
        {
            records = records.Where(record => record.ActorId == actor);
        }

        return records;
    }

    private static int NormalizeTake(int take) => take is < 1 ? 50 : Math.Min(take, MaxTake);
}

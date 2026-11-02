using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// EF Core save interceptor that applies the PII redaction pipeline to every <see cref="AuditLog"/>
/// entity before it reaches the database — the central audit-sink integration point (issue #192).
/// Covers every write path (AuditService, SecurityEventLogger command and retry paths, alert
/// resolution) without per-caller wiring. Vault entries and redaction-operation rows are staged on
/// the same context so record, vault, and audit trail persist in one save (and one caller transaction).
/// Also sweeps the universal entity-change audit records (issue #166): their before/after snapshots
/// pass through the same rule pipeline, so automatically captured deltas are redacted exactly like
/// the central audit sink before persistence.
/// </summary>
public sealed class AuditLogPiiRedactionInterceptor(IPiiRedactionService redactionService) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        // ASP.NET Core has no synchronization context and the provider serves cached snapshots, so
        // this is a fast cache read; a cold scope pays one bounded load.
        ApplyRedaction(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyRedactionAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static List<AuditLog> CollectAuditEntries(DbContext? context)
    {
        if (context is not IApplicationDbContext)
        {
            return [];
        }

        return context.ChangeTracker.Entries<AuditLog>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity)
            .ToList();
    }

    private static List<IEntityChangeAuditRecord> CollectEntityChangeEntries(DbContext? context)
    {
        if (context is not IApplicationDbContext)
        {
            return [];
        }

        // Entity-change records are insert-only: they are staged in the same SaveChanges call as the
        // audited mutation, so only Added entries can carry unredacted snapshots.
        return context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added
                            && entry.Entity is IEntityChangeAuditRecord)
            .Select(entry => (IEntityChangeAuditRecord)entry.Entity)
            .ToList();
    }

    private void ApplyRedaction(DbContext? context)
    {
        var auditEntries = CollectAuditEntries(context);
        var entityChangeEntries = CollectEntityChangeEntries(context);
        if (auditEntries.Count == 0 && entityChangeEntries.Count == 0)
        {
            return;
        }

        var applicationDbContext = (IApplicationDbContext)context!;
        foreach (var entry in auditEntries)
        {
            _ = redactionService.ApplyToAuditLogAsync(entry, applicationDbContext, CancellationToken.None).GetAwaiter().GetResult();
        }

        foreach (var entry in entityChangeEntries)
        {
            _ = redactionService.ApplyToEntityChangeAuditAsync(entry, applicationDbContext, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    private async Task ApplyRedactionAsync(DbContext? context, CancellationToken cancellationToken)
    {
        var auditEntries = CollectAuditEntries(context);
        var entityChangeEntries = CollectEntityChangeEntries(context);
        if (auditEntries.Count == 0 && entityChangeEntries.Count == 0)
        {
            return;
        }

        var applicationDbContext = (IApplicationDbContext)context!;
        foreach (var entry in auditEntries)
        {
            _ = await redactionService.ApplyToAuditLogAsync(entry, applicationDbContext, cancellationToken).ConfigureAwait(false);
        }

        foreach (var entry in entityChangeEntries)
        {
            _ = await redactionService.ApplyToEntityChangeAuditAsync(entry, applicationDbContext, cancellationToken).ConfigureAwait(false);
        }
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for authentication flow state data access operations
/// </summary>
public class AuthenticationFlowStateRepository(IApplicationDbContext context) : IAuthenticationFlowStateRepository
{
    private DbSet<AuthenticationFlowStateRecord> FlowStates { get => context.Set<AuthenticationFlowStateRecord>(); }

    public async Task<AuthenticationFlowStateRecord> CreateAsync(AuthenticationFlowStateRecord record, CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        record.CreatedAt = now;
        record.UpdatedAt = now;

        FlowStates.Add(record);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return record;
    }

    public async Task<AuthenticationFlowStateRecord?> GetByFlowIdAsync(Guid flowId, CancellationToken cancellationToken = default)
    {
        return await FlowStates.AsNoTracking().FirstOrDefaultAsync(flow => flow.FlowId == flowId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthenticationFlowStateRecord?> GetForUpdateAsync(Guid flowId, CancellationToken cancellationToken)
    {
        if (context is DbContext db && db.Database.IsRelational())
        {
            if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" ||
                db.Database.CurrentTransaction is not { } transaction ||
                transaction.GetDbTransaction().IsolationLevel != IsolationLevel.ReadCommitted)
            {
                throw new InvalidOperationException("Authentication flow mutation requires a caller-owned read-committed PostgreSQL transaction.");
            }
            var rows = await FlowStates.FromSqlInterpolated(
                $"""SELECT * FROM "gameguild.authentication"."authentication_flow_states" WHERE "flow_id" = {flowId} FOR UPDATE""")
                .AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            return rows.SingleOrDefault();
        }
        return await GetByFlowIdAsync(flowId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthenticationFlowStateRecord> UpdateAsync(AuthenticationFlowStateRecord record, CancellationToken cancellationToken = default)
    {
        record.UpdatedAt = SystemClock.UtcNow;

        FlowStates.Update(record);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return record;
    }

    public async Task AbandonAsync(Guid flowId, DateTime abandonedAt, CancellationToken cancellationToken = default)
    {
        var record = await FlowStates.FirstOrDefaultAsync(flow => flow.FlowId == flowId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.AbandonedAt = abandonedAt;
        record.UpdatedAt = SystemClock.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteExpiredAsync(DateTime expiredBefore, CancellationToken cancellationToken = default)
    {
        var expired = await FlowStates.Where(flow => flow.ExpiresAt < expiredBefore).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (expired.Count == 0)
        {
            return;
        }

        FlowStates.RemoveRange(expired);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

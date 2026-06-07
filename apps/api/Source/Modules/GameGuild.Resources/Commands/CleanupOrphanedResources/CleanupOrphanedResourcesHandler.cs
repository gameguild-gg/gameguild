using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources;

/// <summary>
///     Handler for CleanupOrphanedResourcesCommand.
/// </summary>
public sealed class CleanupOrphanedResourcesHandler(IApplicationDbContext context)
    : ICommandHandler<CleanupOrphanedResourcesCommand, int>
{
    public async Task<int> Handle(CleanupOrphanedResourcesCommand request, CancellationToken cancellationToken)
    {
        var query = context.Set<UsageRecord>()
            .Where(record => record.TenantId == null || record.TenantId == Guid.Empty);

        if (request.ResourceTypes is { Count: > 0 })
        {
            query = query.Where(record => request.ResourceTypes.Contains(record.Type));
        }

        var records = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (records.Count == 0 || request.DryRun)
        {
            return records.Count;
        }

        context.Set<UsageRecord>().RemoveRange(records);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return records.Count;
    }
}

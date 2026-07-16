using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.LaunchPad;

public sealed class LaunchPadProjectLifecycleParticipant(IApplicationDbContext context)
    : IProjectLifecycleParticipant
{
    public async Task CloseAsync(
        Guid projectId,
        DateTime closedAt,
        CancellationToken cancellationToken = default)
    {
        var activePlans = await context.Set<LaunchPlan>()
            .Where(plan => plan.ProjectId == projectId && plan.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var plan in activePlans)
        {
            plan.DeletedAt = closedAt;
            plan.Touch();
        }
    }
}

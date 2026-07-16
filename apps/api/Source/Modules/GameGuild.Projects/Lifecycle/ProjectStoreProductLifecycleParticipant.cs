using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

public sealed class ProjectStoreProductLifecycleParticipant(IApplicationDbContext context)
    : IProjectLifecycleParticipant
{
    public async Task CloseAsync(
        Guid projectId,
        DateTime closedAt,
        CancellationToken cancellationToken = default)
    {
        var activeLinks = await context.Set<ProjectStoreProduct>()
            .Where(link => link.ProjectId == projectId && link.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var link in activeLinks)
        {
            link.DeletedAt = closedAt;
            link.Touch();
        }
    }

    public async Task RemoveAsync(
        Guid projectId,
        DateTime removedAt,
        CancellationToken cancellationToken = default)
    {
        var links = await context.Set<ProjectStoreProduct>()
            .IgnoreQueryFilters()
            .Where(link => link.ProjectId == projectId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        context.Set<ProjectStoreProduct>().RemoveRange(links);
    }
}

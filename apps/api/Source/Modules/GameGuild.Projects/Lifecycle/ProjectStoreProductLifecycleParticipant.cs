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
}

using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

public sealed class ProjectLifecycleCoordinator(
    IApplicationDbContext context,
    IEnumerable<IProjectLifecycleParticipant> participants,
    IProjectLifecycleLock? lifecycleLock = null) : IProjectLifecycleCoordinator
{
    private readonly IProjectLifecycleLock _lifecycleLock = lifecycleLock ?? new ProjectLifecycleLock(context);

    public async Task<bool> DeleteAsync(
        Guid projectId,
        bool softDelete,
        CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await _lifecycleLock.AcquireAsync(projectId, cancellationToken).ConfigureAwait(false);
        var project = await context.Set<Project>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == projectId && candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (project == null) return false;

        var closedAt = SystemClock.UtcNow;
        foreach (var participant in participants)
            await participant.CloseAsync(projectId, closedAt, cancellationToken).ConfigureAwait(false);

        if (softDelete)
            project.DeletedAt = closedAt;
        else
            context.Set<Project>().Remove(project);

        project.Touch();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await lockHandle.CommitAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}

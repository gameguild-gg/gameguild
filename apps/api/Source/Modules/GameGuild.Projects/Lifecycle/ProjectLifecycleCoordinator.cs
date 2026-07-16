using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Projects;

public sealed class ProjectLifecycleCoordinator(
    IApplicationDbContext context,
    IEnumerable<IProjectLifecycleParticipant> participants) : IProjectLifecycleCoordinator
{
    public async Task<bool> DeleteAsync(
        Guid projectId,
        bool softDelete,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
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
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (context is not DbContext dbContext ||
            dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" ||
            dbContext.Database.CurrentTransaction != null)
        {
            return null;
        }

        return await context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }
}

namespace GameGuild.Projects;

public interface IProjectLifecycleParticipant
{
    Task CloseAsync(Guid projectId, DateTime closedAt, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid projectId, DateTime removedAt, CancellationToken cancellationToken = default)
        => CloseAsync(projectId, removedAt, cancellationToken);
}

public interface IProjectLifecycleCoordinator
{
    Task<bool> DeleteAsync(Guid projectId, bool softDelete, CancellationToken cancellationToken = default);
}

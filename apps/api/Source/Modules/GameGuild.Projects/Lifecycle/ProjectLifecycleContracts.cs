namespace GameGuild.Projects;

public interface IProjectLifecycleParticipant
{
    Task CloseAsync(Guid projectId, DateTime closedAt, CancellationToken cancellationToken = default);
}

public interface IProjectLifecycleCoordinator
{
    Task<bool> DeleteAsync(Guid projectId, bool softDelete, CancellationToken cancellationToken = default);
}

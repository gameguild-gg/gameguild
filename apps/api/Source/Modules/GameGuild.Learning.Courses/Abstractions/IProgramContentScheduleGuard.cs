namespace GameGuild.Learning.Courses;

public interface IProgramContentScheduleGuard
{
    Task<bool> HasActiveScheduleReference(Guid contentId, CancellationToken cancellationToken = default);
}

internal sealed class NullProgramContentScheduleGuard : IProgramContentScheduleGuard
{
    public Task<bool> HasActiveScheduleReference(Guid contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}

namespace GameGuild.Learning.Courses;

/// <summary>
/// Allows feature modules to prevent content lifecycle changes while they own active references.
/// </summary>
public interface IProgramContentLifecycleGuard
{
    Task<bool> HasBlockingDeleteReference(Guid contentId, CancellationToken cancellationToken = default);

    Task<bool> HasBlockingIncompatibleUpdateReference(
        Guid contentId,
        ProgramContentType nextType,
        LessonContentFormat? nextLessonFormat,
        CancellationToken cancellationToken = default);
}

internal sealed class NullProgramContentLifecycleGuard : IProgramContentLifecycleGuard
{
    public Task<bool> HasBlockingDeleteReference(Guid contentId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> HasBlockingIncompatibleUpdateReference(
        Guid contentId,
        ProgramContentType nextType,
        LessonContentFormat? nextLessonFormat,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}

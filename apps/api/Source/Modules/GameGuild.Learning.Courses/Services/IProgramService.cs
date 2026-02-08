namespace GameGuild.Learning.Courses;

/// <summary>
/// Composite interface that combines all program service responsibilities.
/// Kept for backward compatibility — new code should depend on the focused interfaces:
/// <see cref="IProgramCrudService"/>, <see cref="IProgramLifecycleService"/>,
/// <see cref="IProgramContentService"/>, <see cref="IProgramEnrollmentService"/>.
/// </summary>
public interface IProgramService : IProgramCrudService, IProgramLifecycleService { }

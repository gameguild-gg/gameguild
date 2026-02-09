using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get program enrollments </summary>
public sealed record GetProgramEnrollmentsQuery(Guid ProgramId, int Skip = 0, int Take = 50, bool OnlyActive = true) : IQuery<IEnumerable<ProgramUser>>;

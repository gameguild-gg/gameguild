using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get programs by category </summary>
public sealed record GetProgramsByCategoryQuery(ProgramCategory Category, int Skip = 0, int Take = 50, bool OnlyPublished = true) : IQuery<IEnumerable<Program>>;

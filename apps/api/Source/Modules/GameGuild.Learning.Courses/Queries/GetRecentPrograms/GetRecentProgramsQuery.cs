using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get recent programs </summary>
public sealed record GetRecentProgramsQuery(int Skip = 0, int Take = 10, int DaysBack = 7) : IQuery<IEnumerable<Program>>;

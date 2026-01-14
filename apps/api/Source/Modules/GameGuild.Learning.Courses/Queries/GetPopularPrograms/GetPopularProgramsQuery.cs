using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get popular programs </summary>
public record GetPopularProgramsQuery(int Skip = 0, int Take = 10, int DaysBack = 30) : IQuery<IEnumerable<Program>>;

using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get featured programs </summary>
public record GetFeaturedProgramsQuery(int Skip = 0, int Take = 10) : IQuery<IEnumerable<Program>>;

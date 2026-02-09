using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get program ratings </summary>
public sealed record GetProgramRatingsQuery(Guid ProgramId, int Skip = 0, int Take = 50) : IQuery<IEnumerable<ProgramRating>>;

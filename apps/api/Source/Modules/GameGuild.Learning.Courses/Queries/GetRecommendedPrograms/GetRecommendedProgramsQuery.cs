using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get recommended programs for user </summary>
public sealed record GetRecommendedProgramsQuery(string UserId, int Take = 10) : IQuery<IEnumerable<Program>>;

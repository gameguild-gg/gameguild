using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get recommended programs for user </summary>
public record GetRecommendedProgramsQuery(string UserId, int Take = 10) : IQuery<IEnumerable<Program>>;

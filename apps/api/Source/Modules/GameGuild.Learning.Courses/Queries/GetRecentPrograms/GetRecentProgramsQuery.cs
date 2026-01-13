using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get recent programs </summary>
public record GetRecentProgramsQuery(int Skip = 0, int Take = 10, int DaysBack = 7) : IQuery<IEnumerable<Program>>;

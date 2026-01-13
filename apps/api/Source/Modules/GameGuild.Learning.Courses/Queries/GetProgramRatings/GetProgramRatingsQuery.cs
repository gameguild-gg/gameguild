using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get program ratings </summary>
public record GetProgramRatingsQuery(Guid ProgramId, int Skip = 0, int Take = 50) : IQuery<IEnumerable<ProgramRating>>;

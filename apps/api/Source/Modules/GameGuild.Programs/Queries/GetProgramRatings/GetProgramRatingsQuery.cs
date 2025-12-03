using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get program ratings </summary>
public record GetProgramRatingsQuery(Guid ProgramId, int Skip = 0, int Take = 50) : IQuery<IEnumerable<ProgramRating>>;

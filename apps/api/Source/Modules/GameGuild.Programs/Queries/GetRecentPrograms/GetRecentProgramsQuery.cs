using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get recent programs </summary>
public record GetRecentProgramsQuery(int Skip = 0, int Take = 10, int DaysBack = 7) : IQuery<IEnumerable<Program>>;

using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get popular programs </summary>
public record GetPopularProgramsQuery(int Skip = 0, int Take = 10, int DaysBack = 30) : IQuery<IEnumerable<Program>>;

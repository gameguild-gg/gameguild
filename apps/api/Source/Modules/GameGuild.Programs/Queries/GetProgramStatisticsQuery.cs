using GameGuild.CQRS;
using GameGuild.Modules.Programs.DTOs;

namespace GameGuild.Modules.Programs.Queries;

public record GetProgramStatisticsQuery(Guid ProgramId) : IQuery<ProgramStatistics>;

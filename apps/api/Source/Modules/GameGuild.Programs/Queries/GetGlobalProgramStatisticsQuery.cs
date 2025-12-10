using GameGuild.CQRS;
using GameGuild.Modules.Programs.DTOs;

namespace GameGuild.Modules.Programs.Queries;

public record GetGlobalProgramStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<GlobalProgramStatistics>;

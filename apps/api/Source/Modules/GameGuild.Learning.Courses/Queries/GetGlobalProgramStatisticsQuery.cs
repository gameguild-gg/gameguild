using GameGuild.CQRS;

namespace GameGuild.Programs;

public record GetGlobalProgramStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<GlobalProgramStatistics>;

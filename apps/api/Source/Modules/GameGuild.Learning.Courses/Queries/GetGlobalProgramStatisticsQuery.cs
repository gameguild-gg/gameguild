using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public record GetGlobalProgramStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<GlobalProgramStatistics>;

using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get global program statistics </summary>
public record GetGlobalProgramStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<GlobalProgramStatistics>;

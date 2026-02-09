using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get global program statistics </summary>
public sealed record GetGlobalProgramStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<GlobalProgramStatistics>;

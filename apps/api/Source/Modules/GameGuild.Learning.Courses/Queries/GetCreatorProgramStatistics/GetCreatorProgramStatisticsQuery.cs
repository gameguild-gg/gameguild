using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get creator's program statistics </summary>
public record GetCreatorProgramStatisticsQuery(Guid CreatorId, DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<CreatorProgramStatistics>;

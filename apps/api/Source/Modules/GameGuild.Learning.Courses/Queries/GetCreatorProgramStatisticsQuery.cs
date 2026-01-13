using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public record GetCreatorProgramStatisticsQuery(Guid CreatorId, DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<CreatorProgramStatistics>;

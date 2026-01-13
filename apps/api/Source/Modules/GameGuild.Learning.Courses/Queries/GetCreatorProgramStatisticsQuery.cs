using GameGuild.CQRS;

namespace GameGuild.Programs;

public record GetCreatorProgramStatisticsQuery(Guid CreatorId, DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<CreatorProgramStatistics>;

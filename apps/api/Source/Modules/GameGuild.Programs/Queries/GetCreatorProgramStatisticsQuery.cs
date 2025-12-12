using GameGuild.CQRS;
using GameGuild.Modules.Programs.DTOs;

namespace GameGuild.Modules.Programs.Queries;

public record GetCreatorProgramStatisticsQuery(Guid CreatorId, DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<CreatorProgramStatistics>;

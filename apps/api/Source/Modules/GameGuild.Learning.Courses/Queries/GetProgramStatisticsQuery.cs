using GameGuild.CQRS;

namespace GameGuild.Programs;

public record GetProgramStatisticsQuery(Guid ProgramId) : IQuery<ProgramStatistics>;

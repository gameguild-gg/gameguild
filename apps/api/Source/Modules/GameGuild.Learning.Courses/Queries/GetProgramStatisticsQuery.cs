using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public record GetProgramStatisticsQuery(Guid ProgramId) : IQuery<ProgramStatistics>;

using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get program statistics </summary>
public record GetProgramStatisticsQuery(Guid ProgramId) : IQuery<ProgramStatistics>;

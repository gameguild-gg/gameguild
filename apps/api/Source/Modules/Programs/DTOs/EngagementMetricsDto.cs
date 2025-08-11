namespace GameGuild.Modules.Programs;

public record EngagementMetricsDto(
  Guid ProgramId,
  int DailyActiveUsers,
  int WeeklyActiveUsers,
  int MonthlyActiveUsers,
  TimeSpan AverageSessionDuration,
  int TotalSessions,
  decimal RetentionRate,
  Dictionary<string, int> ContentEngagement
) {
  public Guid ProgramId { get; init; } = ProgramId;

  public int DailyActiveUsers { get; init; } = DailyActiveUsers;

  public int WeeklyActiveUsers { get; init; } = WeeklyActiveUsers;

  public int MonthlyActiveUsers { get; init; } = MonthlyActiveUsers;

  public TimeSpan AverageSessionDuration { get; init; } = AverageSessionDuration;

  public int TotalSessions { get; init; } = TotalSessions;

  public decimal RetentionRate { get; init; } = RetentionRate;

  public Dictionary<string, int> ContentEngagement { get; init; } = ContentEngagement;
}

namespace GameGuild.Modules.Programs.DTOs;

public record ProgramAnalyticsDto(
  Guid ProgramId,
  string Title,
  int TotalUsers,
  int ActiveUsers,
  int CompletedUsers,
  decimal CompletionRate,
  TimeSpan AverageCompletionTime,
  int TotalViews,
  DateTime? LastActivity,
  Dictionary<string, object> AdditionalMetrics
) {
  public Guid ProgramId { get; init; } = ProgramId;

  public string Title { get; init; } = Title;

  public int TotalUsers { get; init; } = TotalUsers;

  public int ActiveUsers { get; init; } = ActiveUsers;

  public int CompletedUsers { get; init; } = CompletedUsers;

  public decimal CompletionRate { get; init; } = CompletionRate;

  public TimeSpan AverageCompletionTime { get; init; } = AverageCompletionTime;

  public int TotalViews { get; init; } = TotalViews;

  public DateTime? LastActivity { get; init; } = LastActivity;

  public Dictionary<string, object> AdditionalMetrics { get; init; } = AdditionalMetrics;
}

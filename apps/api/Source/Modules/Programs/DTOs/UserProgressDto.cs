namespace GameGuild.Modules.Programs;

public record UserProgressDto(
  decimal CompletionPercentage,
  DateTime? LastAccessedAt,
  DateTime? StartedAt,
  DateTime? CompletedAt,
  IEnumerable<ContentProgressDto> ContentProgress
) {
  public decimal CompletionPercentage { get; init; } = CompletionPercentage;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public DateTime? StartedAt { get; init; } = StartedAt;

  public DateTime? CompletedAt { get; init; } = CompletedAt;

  public IEnumerable<ContentProgressDto> ContentProgress { get; init; } = ContentProgress;
}

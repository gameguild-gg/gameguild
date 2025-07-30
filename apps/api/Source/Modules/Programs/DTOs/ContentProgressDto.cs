using GameGuild.Common;


namespace GameGuild.Modules.Programs;

public record ContentProgressDto(
  Guid ContentId,
  string Title,
  ProgressStatus Status,
  decimal CompletionPercentage,
  DateTime? FirstAccessedAt,
  DateTime? LastAccessedAt,
  DateTime? CompletedAt
) {
  public Guid ContentId { get; init; } = ContentId;

  public string Title { get; init; } = Title;

  public ProgressStatus Status { get; init; } = Status;

  public decimal CompletionPercentage { get; init; } = CompletionPercentage;

  public DateTime? FirstAccessedAt { get; init; } = FirstAccessedAt;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public DateTime? CompletedAt { get; init; } = CompletedAt;
}

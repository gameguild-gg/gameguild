using GameGuild.Common;


namespace GameGuild.Modules.Programs.DTOs;

public record UpdateProgressDto(
  ProgressStatus? Status = null,
  DateTime? LastAccessedAt = null,
  Dictionary<string, object>? AdditionalData = null
) {
  public ProgressStatus? Status { get; init; } = Status;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public Dictionary<string, object>? AdditionalData { get; init; } = AdditionalData;
}

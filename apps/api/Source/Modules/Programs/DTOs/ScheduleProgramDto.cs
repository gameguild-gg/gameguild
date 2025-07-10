namespace GameGuild.Modules.Programs.DTOs;

public record ScheduleProgramDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

namespace GameGuild.Modules.Programs.DTOs;

public record SchedulePublishDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

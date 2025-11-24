namespace GameGuild.Modules.Programs;

public record SchedulePublishDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

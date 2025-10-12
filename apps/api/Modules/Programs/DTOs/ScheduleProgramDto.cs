namespace GameGuild.Modules.Programs;

public record ScheduleProgramDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

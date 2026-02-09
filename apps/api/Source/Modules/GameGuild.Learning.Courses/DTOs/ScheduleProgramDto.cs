namespace GameGuild.Learning.Courses;

public sealed record ScheduleProgramDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

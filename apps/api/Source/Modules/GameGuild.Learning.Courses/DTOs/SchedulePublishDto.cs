namespace GameGuild.Learning.Courses;

public sealed record SchedulePublishDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

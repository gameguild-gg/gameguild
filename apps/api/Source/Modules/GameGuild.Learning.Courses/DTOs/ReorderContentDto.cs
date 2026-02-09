namespace GameGuild.Learning.Courses;

public sealed record ReorderContentDto(List<Guid> ContentIds) {
  public List<Guid> ContentIds { get; init; } = ContentIds;
}

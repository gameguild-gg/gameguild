namespace GameGuild.Learning.Courses;

public record ReorderContentDto(List<Guid> ContentIds) {
  public List<Guid> ContentIds { get; init; } = ContentIds;
}



namespace GameGuild.Learning.Courses;

public sealed record BulkUpdateProgramsDto(List<Guid> ProgramIds, ContentStatus? Status = null, ContentVisibility? Visibility = null) {
  public List<Guid> ProgramIds { get; init; } = ProgramIds;

  public ContentStatus? Status { get; init; } = Status;

  public ContentVisibility? Visibility { get; init; } = Visibility;
}

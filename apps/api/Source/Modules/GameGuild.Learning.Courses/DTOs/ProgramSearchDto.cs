

namespace GameGuild.Learning.Courses;

public sealed record ProgramSearchDto(string? SearchTerm = null, ContentStatus? Status = null, ContentVisibility? Visibility = null, Guid? CreatorId = null, int Skip = 0, int Take = 50) {
  public string? SearchTerm { get; init; } = SearchTerm;

  public ContentStatus? Status { get; init; } = Status;

  public ContentVisibility? Visibility { get; init; } = Visibility;

  public Guid? CreatorId { get; init; } = CreatorId;

  public int Skip { get; init; } = Skip;

  public int Take { get; init; } = Take;
}

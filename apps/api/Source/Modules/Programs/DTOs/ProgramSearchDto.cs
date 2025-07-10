using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Programs.DTOs;

public record ProgramSearchDto(
  string? SearchTerm = null,
  ContentStatus? Status = null,
  AccessLevel? Visibility = null,
  Guid? CreatorId = null,
  int Skip = 0,
  int Take = 50
) {
  public string? SearchTerm { get; init; } = SearchTerm;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;

  public Guid? CreatorId { get; init; } = CreatorId;

  public int Skip { get; init; } = Skip;

  public int Take { get; init; } = Take;
}

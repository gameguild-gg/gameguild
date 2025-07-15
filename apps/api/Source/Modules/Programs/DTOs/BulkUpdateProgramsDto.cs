using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Programs;

public record BulkUpdateProgramsDto(
  List<Guid> ProgramIds,
  ContentStatus? Status = null,
  AccessLevel? Visibility = null
) {
  public List<Guid> ProgramIds { get; init; } = ProgramIds;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;
}

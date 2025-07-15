namespace GameGuild.Modules.Programs;

public record ReorderContentDto(List<Guid> ContentIds) {
  public List<Guid> ContentIds { get; init; } = ContentIds;
}

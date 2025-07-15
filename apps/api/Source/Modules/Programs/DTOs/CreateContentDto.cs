namespace GameGuild.Modules.Programs;

public record CreateContentDto(
  string Title,
  string Description,
  Common.ProgramContentType Type,
  string Body,
  int? SortOrder = null,
  bool IsRequired = true,
  int? EstimatedMinutes = null
) {
  public string Title { get; init; } = Title;

  public string Description { get; init; } = Description;

  public Common.ProgramContentType Type { get; init; } = Type;

  public string Body { get; init; } = Body;

  public int? SortOrder { get; init; } = SortOrder;

  public bool IsRequired { get; init; } = IsRequired;

  public int? EstimatedMinutes { get; init; } = EstimatedMinutes;
}

using GameGuild.Common;


namespace GameGuild.Modules.Programs.DTOs;

public record CreateContentDto(
  string Title,
  string Description,
  ProgramContentType Type,
  string Body,
  int? SortOrder = null,
  bool IsRequired = true,
  int? EstimatedMinutes = null
) {
  public string Title { get; init; } = Title;

  public string Description { get; init; } = Description;

  public ProgramContentType Type { get; init; } = Type;

  public string Body { get; init; } = Body;

  public int? SortOrder { get; init; } = SortOrder;

  public bool IsRequired { get; init; } = IsRequired;

  public int? EstimatedMinutes { get; init; } = EstimatedMinutes;
}

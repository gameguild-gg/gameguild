namespace GameGuild.Modules.Programs.DTOs;

public record UpdateContentDto(
  string? Title = null,
  string? Description = null,
  string? Body = null,
  int? SortOrder = null,
  bool? IsRequired = null,
  int? EstimatedMinutes = null
) {
  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? Body { get; init; } = Body;

  public int? SortOrder { get; init; } = SortOrder;

  public bool? IsRequired { get; init; } = IsRequired;

  public int? EstimatedMinutes { get; init; } = EstimatedMinutes;
}

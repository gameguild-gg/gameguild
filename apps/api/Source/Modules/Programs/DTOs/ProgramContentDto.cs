using System.Text.Json;
using GameGuild.Common;


namespace GameGuild.Modules.Programs;

/// <summary>
/// DTO for program content responses
/// </summary>
public class ProgramContentDto {
  public Guid Id { get; set; }

  public Guid ProgramId { get; set; }

  public Guid? ParentId { get; set; }

  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public Common.ProgramContentType Type { get; set; }

  public JsonDocument? Body { get; set; }

  public int SortOrder { get; set; }

  public bool IsRequired { get; set; }

  public GradingMethod? GradingMethod { get; set; }

  public decimal? MaxPoints { get; set; }

  public int? EstimatedMinutes { get; set; }

  public Visibility Visibility { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }

  // Navigation properties
  public string? ProgramTitle { get; set; }

  public string? ParentTitle { get; set; }

  public int ChildrenCount { get; set; }

  public List<ProgramContentDto> Children { get; set; } = new();
}

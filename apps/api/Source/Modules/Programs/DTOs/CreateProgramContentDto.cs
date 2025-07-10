using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// DTO for creating new program content
/// </summary>
public class CreateProgramContentDto {
  [Required] public Guid ProgramId { get; set; }

  public Guid? ParentId { get; set; }

  [Required] [StringLength(255)] public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  [Required] public ProgramContentType Type { get; set; }

  public string Body { get; set; } = "{}";

  public int SortOrder { get; set; } = 0;

  public bool IsRequired { get; set; } = true;

  public GradingMethod? GradingMethod { get; set; }

  public decimal? MaxPoints { get; set; }

  public int? EstimatedMinutes { get; set; }

  public Visibility Visibility { get; set; } = Visibility.Published;
}
